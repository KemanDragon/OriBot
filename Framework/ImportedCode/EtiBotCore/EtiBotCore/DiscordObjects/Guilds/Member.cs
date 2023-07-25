using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Container;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds.MemberData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Exceptions;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;

namespace EtiBotCore.DiscordObjects.Guilds {

	/// <summary>
	/// Represents a <see cref="Universal.User"/> but in a specific <see cref="Guild"/>.
	/// </summary>
	
	public class Member : User, IEquatable<Member> {

		internal static readonly ThreadedDictionary<Snowflake, ThreadedDictionary<Snowflake, Member>> InstantiatedMembers = new ThreadedDictionary<Snowflake, ThreadedDictionary<Snowflake, Member>>();

		#region Main Member Properties

		/// <summary>
		/// The base user of this member. Identical to <see langword="this"/> (because this object extends User).
		/// </summary>
		public User User => this;

		/// <summary>
		/// The server that this <see cref="Member"/> is a member of.
		/// </summary>
		public Guild Server { get; }

		/// <summary>
		/// The member's nickname. This will never return an empty string, and will always return <see langword="null"/> for members with no nickname.
		/// </summary>
		/// <exception cref="PropertyLockedException"></exception>
		/// <exception cref="InsufficientPermissionException"></exception>
		public string? Nickname {
			get {
				if (string.IsNullOrWhiteSpace(_Nickname)) {
					return null;
				}
				return _Nickname;
			}
			set {
				if (IsSelf) EnforcePermissions(Server, Permissions.ChangeNickname);
				else EnforcePermissions(Server, Permissions.ManageNicknames);
				
				SetProperty(ref _Nickname, value);
			}
		}
		internal string? _Nickname = null;

		/// <summary>
		/// The roles this member has. This class can be viewed or changed like a list or array: <c>Roles[int] = Role</c>
		/// </summary>
		/// <remarks>
		/// <strong>This reference is cloned in clone objects.</strong>
		/// </remarks>
		/// <exception cref="PropertyLockedException"></exception>
		/// <exception cref="InsufficientPermissionException"></exception>
		public DiscordObjectContainer<Role> Roles {
			get {
				if (_Roles == null) {
					_Roles = new DiscordObjectContainer<Role>(this, true, false) {
						DeleteIfRemoved = false
					};
				}
				return _Roles;
			}
		}
		private DiscordObjectContainer<Role>? _Roles = null;
		
		/// <summary>
		/// When this member joined the server.
		/// </summary>
		public DateTimeOffset JoinedAt { get; internal set; }

		/// <summary>
		/// When this user purchased Nitro, or <see langword="null"/> if they do not have Nitro.
		/// </summary>
		public DateTimeOffset? PremiumSince { get; internal set; }

		/// <summary>
		/// Whether or not this member has been muted in voice chats.
		/// </summary>
		/// <exception cref="PropertyLockedException"></exception>
		/// <exception cref="InvalidOperationException">If this member is not the bot.</exception>
		public bool Muted {
			get => _Muted;
			set {
				if (!IsSelf) EnforcePermissions(Server, Permissions.MuteMembers);
				SetProperty(ref _Muted, value);
			}
		}
		private bool _Muted = false;

		/// <summary>
		/// Whether or not this member has been deafened in voice chats.
		/// </summary>
		/// <exception cref="PropertyLockedException"></exception>
		/// <exception cref="InvalidOperationException">If this member is not the bot.</exception>
		public bool Deafened {
			get => _Deafened;
			set {
				if (!IsSelf) EnforcePermissions(Server, Permissions.DeafenMembers);
				SetProperty(ref _Deafened, value);
			}
		}
		private bool _Deafened = false;

		/// <summary>
		/// The presence of this member. If the bot is unauthorized to view presences or has not received this member's presence, this will return an offline presence.
		/// </summary>
		/// <remarks>
		/// <strong>This reference is cloned in clone objects.</strong>
		/// </remarks>
		public Presence Presence {
			get {
				if (_Presence == null) {
					_Presence = Server.Presences.FirstOrDefault(presence => presence.UserID == ID);
					if (_Presence == null) {
						_Presence = Presence.CreateOfflinePresence(this);
					}
				}
				return _Presence;
			} 
		}
		private Presence? _Presence = null;

		/// <summary>
		/// Whether or not this member has passed the rules screening check required to get into the server.
		/// </summary>
		public bool IsPending { get; private set; }

		/// <summary>
		/// The voice channel this member is connected to.
		/// </summary>
		/// <exception cref="PropertyLockedException">If the member object is not expecting changes.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot move members.</exception>
		/// <exception cref="ObjectDeletedException">If the member left the server.</exception>
		public VoiceChannel? CurrentVoiceChannel {
			get {
				if (!HasGrabbedVoiceChannel) {
					HasGrabbedVoiceChannel = true;
					_CurrentVoiceChannel = VoiceState.Channel as VoiceChannel;
				}
				return _CurrentVoiceChannel;
			}
			set {
				if (LeftServer) throw new ObjectDeletedException(this);
				EnforcePermissions(Server, Permissions.MoveMembers);
				HasGrabbedVoiceChannel = true;
				SetProperty(ref _CurrentVoiceChannel, value);
			}
		}
		internal VoiceChannel? _CurrentVoiceChannel = null;
		private bool HasGrabbedVoiceChannel = false;

		#endregion

		#region Extended Properties

		/// <summary>
		/// If true, this member has left the server they exist in. Identical to <see cref="DiscordObject.Deleted"/> (which is set to <see langword="true"/> if they leave)
		/// </summary>
		public bool LeftServer => Deleted;

		/// <summary>
		/// An extension of <see cref="User.FullName"/> that includes this member's nickname (if applicable) in the format <c>Nickname (FullName)</c>.
		/// </summary>
		public string FullNickname {
			get {
				if (Nickname == null) {
					return FullName;
				}
				return $"{Nickname} ({FullName})";
			}
		}

		/// <summary>
		/// If <see langword="true"/>, this member was not created inside of the member added event. As a result, any and all server-dependent info (with the exception of the server itself) will be missing.
		/// </summary>
		/// <remarks>
		/// While the server-dependent information will be unusable, all <see cref="Universal.User"/> properties will be usable. As such, it is advised to reference <see cref="User"/> when addressing properties as to not accidentally use missing properties.
		/// </remarks>
		public bool IsShallow { get; internal set; } = false;

		/// <summary>
		/// The permissions this <see cref="Member"/> has across the entire server based on the roles they have. This does not factor in any channel-specific permissions.
		/// If this member is an administrator, this returns <see cref="Permissions.All"/>
		/// </summary>
		public Permissions AllowedServerPermissions {
			get {
				Permissions allowed = Permissions.None;
				foreach (Role? role in Roles) {
					Permissions allowedByRole = role!.Permissions.GetAllowed();
					if (allowedByRole.HasFlag(Permissions.Administrator)) {
						return Permissions.All;
					}
					allowed |= allowedByRole;
				}
				return allowed;
			}
		}

		/// <summary>
		/// Returns whether or not this user is an administrator because one or more of their roles has the Administrator permission.
		/// </summary>
		public bool IsAdministrator {
			get {
				foreach (Role? role in Roles) {
					Permissions allowedByRole = role!.Permissions.GetAllowed();
					if (allowedByRole.HasFlag(Permissions.Administrator)) {
						return true;
					}
				}
				return false;
			}
		}

		#endregion

		#region Extended Methods

		#region For Permissions

		/// <summary>
		/// Returns the permissions that are allowed in the given channel. If this member has administrator, then <see cref="Permissions.All"/> is returned.
		/// </summary>
		/// <remarks>
		/// This returns the effective permissions from their roles and the channel's overwrites to any of those roles and/or their specific user.
		/// If your goal is to acquire the overrides specifically defined just for this user explicitly, acquire it from the channel itself.<para/>
		/// <para/>
		/// If this is called on a thread, this acts on the permissions of the parent channel (as it should), and is identical to passing in thread.ParentChannel.
		/// </remarks>
		/// <param name="channel"></param>
		/// <returns></returns>
		/// <exception cref="ObjectUnavailableException">If the server this channel is in is experiencing an outage.</exception>
		/// <exception cref="ArgumentException">If the channel is not in the same server as this member object.</exception>
		public Permissions GetPermissionsInChannel(GuildChannelBase channel) {
			if (channel.Server!.Unavailable) throw new ObjectUnavailableException(channel.Server);
			if (channel.Server != Server) throw new ArgumentException("The channel is from a different server than this member!");
			Permissions basePerms = AllowedServerPermissions;
			if (basePerms.HasFlag(Permissions.Administrator)) return Permissions.All;
			Permissions result = basePerms;
			PermissionContainer objectPerms = channel.Permissions;

			// Start with the overwrites for @everyone
			PermissionInformation? everyonePerms = objectPerms.GetPermission(Server.EveryoneRole);
			if (everyonePerms != null) {
				result = everyonePerms.ApplyTo(result);
			}

			// Now all other overwrites.
			// Roles first. We only care about the roles that the member has.
			Permissions allow = Permissions.None;
			Permissions deny = Permissions.None;
			foreach (Role role in Roles) {
				PermissionInformation? rolePerms = objectPerms.GetPermission(role);
				if (rolePerms == null) continue;

				allow |= rolePerms.GetAllowed();
				deny |= rolePerms.GetDenied();
			}

			result &= ~deny;
			result |= allow;

			// And now member-specific stuff.
			PermissionInformation? specificPerms = objectPerms.GetPermission(this);
			if (specificPerms != null) {
				result &= ~specificPerms.GetDenied();
				result |= specificPerms.GetAllowed();
			}

			return result;
		}

		/// <summary>
		/// Determines whether or not the member has the given permissions. This checks for the administrator permission automatically, and if this member has it, will always return true.
		/// </summary>
		/// <param name="perms"></param>
		/// <returns></returns>
		public bool HasPermission(Permissions perms) {
			return IsAdministrator || AllowedServerPermissions.HasFlag(perms);
		}

		#endregion

		#region Server Moderation

		/// <summary>
		/// Kicks this member for the given reason.
		/// </summary>
		/// <param name="reason">Why are you kicking this member?</param>
		/// <returns></returns>
		public Task KickAsync(string? reason) {
			return Server.KickMemberAsync(this, reason);
		}

		/// <summary>
		/// Ban this member for the given reason, and delete the messages they sent in the past <paramref name="deleteMessageDays"/> days.<para/><para/>
		/// <code>Administer last rites, sir?</code>
		/// </summary>
		/// <param name="reason">Why are you banning this member?</param>
		/// <param name="deleteMessageDays">Delete messages they sent up to this many days beforehand.</param>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the BAN_MEMBERS permission.</exception>
		public Task BanAsync(string? reason, int deleteMessageDays = 0) {
			return Server.BanMemberAsync(this, reason, deleteMessageDays);
		}

		#endregion

		#region For Threads

		/// <summary>
		/// Returns whether or not this member has the ability to chat in the given thread.
		/// </summary>
		/// <param name="thread">The thread to test.</param>
		/// <returns></returns>
		public bool CanChatInThread(Thread thread) {
			if (IsAdministrator) return true;

			Permissions permsInParent = GetPermissionsInChannel(thread);
			if (permsInParent.HasFlag(Permissions.ManageThreads)) return true;

			bool canSendMessages = permsInParent.HasFlag(Permissions.SendMessages);
			if (!canSendMessages) {
				// Cannot send messages. Capabilities rely on whether or not a thread is public.
				// If a thread is private, they must be able to see it.
				if (thread.IsPublic) {
					return permsInParent.HasFlag(Permissions.UsePublicThreads);
				} else if (thread.IsPrivate) {
					return permsInParent.HasFlag(Permissions.UsePrivateThreads) && CanSeeChannel(thread);
				} else {
					throw new InvalidOperationException("The given thread does not have a type dedicated for threads?!");
				}
			} else {
				// Can send messages. In this case, public threads are always accessible (even if "Use Public Threads" is false),
				// and private threads require explicit access.
				if (thread.IsPublic) {
					return true;
				} else if (thread.IsPrivate) {
					return CanSeeChannel(thread);
				} else {
					throw new InvalidOperationException("The given thread does not have a type dedicated for threads?!");
				}
			}
		}
		
		/// <summary>
		/// Returns whether or not this member can see the given channel.
		/// </summary>
		/// <param name="channel">The channel to test.</param>
		/// <returns></returns>
		private bool CanSeeChannel(TextChannel channel) => GetPermissionsInChannel(channel).HasFlag(Permissions.ViewChannel);

		/// <summary>
		/// Returns whether or not this member can create new public threads.
		/// </summary>
		/// <param name="forChannel">The channel that the thread will be a part of.</param>
		/// <returns></returns>
		public bool CanCreatePublicThreads(TextChannel forChannel) {
			Permissions inChannel = GetPermissionsInChannel(forChannel);
			Permissions test = Permissions.ViewChannel | Permissions.SendMessages | Permissions.UsePublicThreads;
			return inChannel.HasFlag(test);
		}

		/// <summary>
		/// Returns whether or not this member can create new private threads.
		/// </summary>
		/// <param name="forChannel">The channel that the thread will be a part of.</param>
		/// <returns></returns>
		public bool CanCreatePrivateThreads(TextChannel forChannel) {
			Permissions inChannel = GetPermissionsInChannel(forChannel);
			Permissions test = Permissions.ViewChannel | Permissions.SendMessages | Permissions.UsePrivateThreads;
			return inChannel.HasFlag(test);
		}

		#endregion

		#endregion

		#region Internals


		/// <summary>
		/// Should only be used in actual member creations, never in message create/update events (because no user exists in those).<para/>
		/// This updates the registry.
		/// </summary>
		/// <param name="sourceGuild">The <see cref="Guild"/> creating this <see cref="Member"/></param>
		/// <param name="member">The member payload</param>
		internal Member(Guild sourceGuild, Payloads.PayloadObjects.Member member) : base(member.User!) {
			Server = sourceGuild;
			if (!InstantiatedMembers.ContainsKey(sourceGuild.ID)) {
				InstantiatedMembers[sourceGuild.ID] = new ThreadedDictionary<Snowflake, Member>();
			}
			InstantiatedMembers[sourceGuild.ID][member.User!.UserID] = this;
			Update(member, false).Wait(); // Okay bc this never awaits
			//DiscordClient.Current!.Events.MemberEvents.InvokeOnGuildMemberAdded(Server, this);
		}

		/// <summary>
		/// Constructs a shallow member.
		/// </summary>
		/// <param name="sourceGuild"></param>
		/// <param name="baseUser"></param>
		internal Member(Guild sourceGuild, Payloads.PayloadObjects.User baseUser) : base(baseUser) {
			Server = sourceGuild;
			if (!InstantiatedMembers.ContainsKey(sourceGuild.ID)) {
				InstantiatedMembers[sourceGuild.ID] = new ThreadedDictionary<Snowflake, Member>();
			}
			InstantiatedMembers[sourceGuild.ID][baseUser.UserID] = this;
			IsShallow = true;
			//DiscordClient.Current!.Events.MemberEvents.InvokeOnGuildMemberAdded(Server, this);
		}

		internal static bool MemberExists(Guild inServer, Snowflake id) => InstantiatedMembers.ContainsKey(inServer.ID) && InstantiatedMembers[inServer.ID].ContainsKey(id);

		/// <summary>
		/// Asynchronously returns an existing cached Member in the given server with the given ID, or downloads this member and constructs a new one.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="inServer"></param>
		/// <returns></returns>
		internal static async Task<Member?> GetOrCreateAsync(Snowflake id, Guild inServer) {
			if (id == Snowflake.Invalid) {
				throw new ArgumentException("The given snowflake is invalid!");
			}
			if (!InstantiatedMembers.ContainsKey(inServer.ID)) {
				InstantiatedMembers[inServer.ID] = new ThreadedDictionary<Snowflake, Member>();
			}
			if (!InstantiatedMembers[inServer.ID].ContainsKey(id)) {

				// return new Member(inServer, id); // Populates the registry itself.
				(var memberPayload, var response) = (await Guild.GetGuildMember.ExecuteAsync<Payloads.PayloadObjects.Member>(new APIRequestData { Params = { inServer.ID, id } }));
				if (response!.IsSuccessStatusCode) {
					return new Member(inServer, memberPayload!);
				} else {
					// A download failed. We need to make a shallow user.
					(var userPayload, _) = await GetUser.ExecuteAsync<Payloads.PayloadObjects.User>(new APIRequestData { Params = { id } });
					if (userPayload != null) {
						return new Member(inServer, userPayload);
					}
					return null;
				}
			}

			return InstantiatedMembers[inServer.ID][id];
		}

		/// <summary>
		/// Creates a new <see cref="Member"/> or gets an existing one by ID. This does not download the member like <see cref="GetOrCreateAsync(Snowflake, Guild)"/>.
		/// Be careful, because if the member hasn't been created before, it will return a shallow member.
		/// </summary>
		/// <param name="inServer"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		internal static Member EventGetOrCreate(Payloads.PayloadObjects.User user, Guild inServer) {
			if (!InstantiatedMembers.ContainsKey(inServer.ID)) {
				InstantiatedMembers[inServer.ID] = new ThreadedDictionary<Snowflake, Member>();
			}
			if (!InstantiatedMembers[inServer.ID].ContainsKey(user.UserID)) {
				InstantiatedMembers[inServer.ID][user.UserID] = new Member(inServer, user);
			}
			return InstantiatedMembers[inServer.ID][user.UserID];
		}

		internal static async Task<Member> CreateFromPayloadInRequestChunk(Payloads.PayloadObjects.Member mbr, Snowflake serverId) {
			Guild server = await Guild.GetOrDownloadAsync(serverId, true);
			if (!InstantiatedMembers.ContainsKey(serverId)) {
				InstantiatedMembers[serverId] = new ThreadedDictionary<Snowflake, Member>();
			}
			if (!InstantiatedMembers[serverId].ContainsKey(mbr.User!.UserID)) {
				InstantiatedMembers[serverId][mbr.User!.UserID] = new Member(server, mbr);
			} else {
				await InstantiatedMembers[serverId][mbr.User!.UserID].Update(mbr, false);
			}
			return InstantiatedMembers[serverId][mbr.User!.UserID];
		}

		#endregion

		#region Implementation

		/// <inheritdoc/>
		protected internal override async Task Update(PayloadDataObject obj, bool skipNonNullFields) {
			if (obj is Payloads.PayloadObjects.User user) {
				// There's a chance this could have been called with a user payload. Handle it as such.
				await base.Update(user, skipNonNullFields);
			} else if (obj is Payloads.PayloadObjects.Member member) {
				if (member.User != null) await base.Update(member.User, skipNonNullFields);

				// Values that are always sent do not need the AppropriateValue call.
				_Nickname = AppropriateValue(Nickname, member.Nickname, skipNonNullFields);
				JoinedAt = AppropriateTime(JoinedAt, member.JoinedAt.DateTime);
				PremiumSince = AppropriateNullableTime(PremiumSince, member.PremiumSince?.DateTime);
				_Muted = member.Muted;
				_Deafened = member.Deafened;
				IsPending = AppropriateNullableValue(IsPending, member.Pending, skipNonNullFields);

				// ROLES //
				List<Role> roles = new List<Role>();
				bool hasTriedDownloading = false;
				foreach (ulong roleId in member.Roles) {
					if (!Server.Roles.Contains(roleId) && !hasTriedDownloading) {
						await Server.RedownloadAllRolesAsync();
						hasTriedDownloading = true;
					}
					if (Server.Roles.Contains(roleId)) {
						roles.Add(Server.Roles[roleId]!);
					} else {
						DiscordClient.Log.WriteWarning($"I can't assign [{roleId}] to {FullName} because it seems to be missing from the server's registry!", EtiLogger.Logging.LogLevel.Debug);
					}
				}
				Roles.SetTo(roles);
			} else if (obj is Payloads.Events.Intents.GuildMembers.GuildMemberUpdateEvent evt) {
				if (evt.User != null) await base.Update(evt.User, skipNonNullFields);

				// Values that are always sent do not need the AppropriateValue call.
				_Nickname = AppropriateValue(Nickname, evt.Nickname, skipNonNullFields);
				JoinedAt = AppropriateTime(JoinedAt, evt.JoinedAt.DateTime);
				PremiumSince = AppropriateNullableTime(PremiumSince, evt.PremiumSince?.DateTime);

				// ROLES //
				List<Role> roles = new List<Role>();
				foreach (ulong roleId in evt.Roles) {
					if (Server.Roles.Contains(roleId)) {
						roles.Add(Server.Roles[roleId]!);
					} else {
						DiscordClient.Log.WriteWarning($"I can't assign [{roleId}] to {FullName} because it seems to be missing from the server's registry!", EtiLogger.Logging.LogLevel.Debug);
					}
				}
				Roles.SetTo(roles);
			} else if (obj is Payloads.Events.Intents.GuildPresences.PresenceUpdateEvent pres) {
				Presence presence = new Presence(pres);
				_Presence = presence;
			}
		}

		/// <inheritdoc/>
		protected override async Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changes, string? reason) {
			// base.SendChangesToDiscord(changes);

			APIRequestData modify = new APIRequestData {
				Params = { Server.ID, ID },
				Reason = reason
			};

			if (changes.ContainsKey(nameof(Nickname))) {
				if (changes.Count == 1 && IsSelf) {
					// Only changing the nickname and I'm changing myself, use the special endpoint.
					APIRequestData modNick = new APIRequestData {
						Params = { Server.ID }
					};
					modNick.SetJsonField("nick", Nickname);
					return await Guild.ModifyCurrentUserNick.ExecuteAsync(modNick);
				}

				// If the code makes it here, we're modifying a guild member.
				modify.SetJsonField("nick", Nickname);
			}
			if (changes.ContainsKey(nameof(Roles))) {
				// Special handling here too -- was just one role added or removed? If so, use the special endpoint.
				if (Roles.HasChangedYet && Roles.SingularChange != null) {
					Roles.HasChangedYet = false;
					Snowflake changedId = Roles.SingularChange.ID;
					Roles.SingularChange = null;

					HttpResponseMessage? msg;
					if (Roles.WasChangeRemoval) {
						msg = await Guild.RemoveGuildMemberRole.ExecuteAsync(new APIRequestData { Params = { Server.ID, ID, changedId } });
					} else {
						msg = await Guild.AddGuildMemberRole.ExecuteAsync(new APIRequestData { Params = { Server.ID, ID, changedId } });
					}
					Roles.Reset();
					return msg;
				}
				modify.SetJsonField("roles", Roles.ToIDArray());
				Roles.Reset();
			}
			if (changes.ContainsKey(nameof(Muted))) modify.SetJsonField("mute", Muted);
			if (changes.ContainsKey(nameof(Deafened))) modify.SetJsonField("deaf", Deafened);
			if (changes.ContainsKey(nameof(CurrentVoiceChannel))) modify.SetJsonField("channel_id", CurrentVoiceChannel?.ID);
			return await Guild.ModifyGuildMember.ExecuteAsync(modify);
		}

		/// <inheritdoc/>
		public static bool operator ==(Member? left, Member? right) {
			if (left is null && right is null) return true;
			if (left is null) {
				return right!.Equals(left);
			} else {
				return left.Equals(right);
			}
		}

		/// <inheritdoc/>
		public static bool operator !=(Member? left, Member? right) => !(left == right);

		/// <inheritdoc/>
		public override bool Equals(object? other) {
			if (other is Member member) return Equals(member);
			return base.Equals(other);
		}

		/// <inheritdoc/>
		public bool Equals([AllowNull] Member other) {
			if (other is null) return false;
			return ID == other.ID && Server.ID == other.Server.ID;
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return HashCode.Combine(base.GetHashCode(), ID);
		}

		#endregion

		/// <inheritdoc/>
		public override DiscordObject MemberwiseClone() {
			Member newMember = (Member)base.MemberwiseClone();
			newMember._Roles = Roles.Clone();
			newMember._Presence = _Presence?.Clone();
			newMember._CurrentVoiceChannel = null;
			return newMember;
			// roles, presence, and voice channel
			// voice channel can be done lazily by unsetting it
		}

	}
}

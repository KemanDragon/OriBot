using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using EtiLogger.Logging;
using OldOriBot.Data;
using OldOriBot.Data.Persistence;
using OldOriBot.PermissionData;

namespace OldOriBot.Interaction {

	/// <summary>
	/// Represents a server.
	/// </summary>
	public abstract class BotContext : IEquatable<BotContext> {

		/// <summary>
		/// The display name of this context.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// The name of this context's data persistence folder.
		/// </summary>
		public abstract string DataPersistenceName { get; }

		/// <summary>
		/// A registry of every stored permission for every member in this context's server.
		/// </summary>
		private readonly Dictionary<Snowflake, PermissionLevel> PermissionRegistry = new Dictionary<Snowflake, PermissionLevel>();

		/// <summary>
		/// The server this <see cref="BotContext"/> represents. This will be <see langword="null"/> if the bot is not in this server.
		/// </summary>
		public Guild Server { get; private set; }

		/// <summary>
		/// The ID of this server.
		/// </summary>
		protected abstract Snowflake ServerID { get; }

		/// <summary>
		/// The ID of this <see cref="BotContext"/>, which is identical to its corresponding server's ID.
		/// </summary>
		public Snowflake ID => ServerID;

		/// <summary>
		/// The commands in this server.
		/// </summary>
		public abstract Command[] Commands { get; set; }

		/// <summary>
		/// The order-sensitive list of <see cref="PassiveHandler"/>s in this server. They are executed from first to last, and execution stops when a handler returns true.
		/// </summary>
		public abstract PassiveHandler[] Handlers { get; set; }

		/// <summary>
		/// Whether or not to download every single member when <see cref="Server"/> is set.
		/// </summary>
		public virtual bool DownloadsAllMembers { get; }

		/// <summary>
		/// The role that belongs to this bot. This may yield while the bot's member is downloaded on the first <see langword="get"/> call.<para/>
		/// In servers where the bot joins without permissions, this will be <see langword="null"/>.
		/// </summary>
		public Role BotRole {
			get {
				if (!HasSearchedForBotRole) {
					foreach (Role role in Server.BotMember.Roles) {
						if (role.Integrated) {
							_BotRole = role;
							break;
						}
					}
					HasSearchedForBotRole = true;
				}
				return _BotRole;
			}
		}
		private Role _BotRole = null;
		private bool HasSearchedForBotRole = false;

		/// <summary>
		/// A <see cref="Logger"/> designed for this <see cref="BotContext"/>.
		/// </summary>
		public Logger ContextLogger {
			get {
				if (_CtxLog == null) {
					_CtxLog = new Logger($"^#80cf53;[Bot Context: ^#5ea88e;{Name}^#80cf53;] ");
				}
				return _CtxLog;
			}
		}
		private Logger _CtxLog = null;


		/// <summary>
		/// The event log channel, where the bot tracks things it does.
		/// </summary>
		public TextChannel EventLog => Server.GetChannel<TextChannel>(EventLogID);

		/// <summary>
		/// The ID of the bot event log.
		/// </summary>
		protected abstract Snowflake EventLogID { get; }

		/// <summary>
		/// The bot membership log, which tracks members leaving and joining the server.
		/// </summary>
		public TextChannel MembershipLog => Server.GetChannel<TextChannel>(MembershipLogID);

		/// <summary>
		/// The ID of the membership logging channel.
		/// </summary>
		protected abstract Snowflake MembershipLogID { get; }

		/// <summary>
		/// A channel that logs message deletions and edits.
		/// </summary>
		public TextChannel MessageBehaviorLog => Server.GetChannel<TextChannel>(MessageBehaviorLogID);

		/// <summary>
		/// The ID of the message edit/deletion logging channel.
		/// </summary>
		protected abstract Snowflake MessageBehaviorLogID { get; }

		/// <summary>
		/// A channel that logs members connecting to or disconnecting from voice.
		/// </summary>
		public TextChannel VoiceBehaviorLog => Server.GetChannel<TextChannel>(VoiceBehaviorLogID);

		/// <summary>
		/// The ID of the voice channel join/leave log.
		/// </summary>
		protected abstract Snowflake VoiceBehaviorLogID { get; }

		/// <summary>
		/// The moderation logging channel. By default, this is just <see cref="EventLog"/>.
		/// </summary>
		public TextChannel ModerationLog => Server.GetChannel<TextChannel>(ModerationLogID);

		/// <summary>
		/// The ID of the moderation log. By default, this is the same as <see cref="EventLogID"/>
		/// </summary>
		protected virtual Snowflake ModerationLogID => EventLogID;

		/// <summary>
		/// The ID of the bot channel in this server. If defined, and if <see cref="OnlyAllowCommandsInBotChannel"/> is <see langword="true"/>, commands will not work in other channels.
		/// </summary>
		public virtual Snowflake? BotChannelID { get; }

		/// <summary>
		/// If <see cref="BotChannelID"/> is appropriately defined, commands will only run in the channel with that given ID for users below <see cref="PermissionLevel.Operator"/>.
		/// </summary>
		public virtual bool OnlyAllowCommandsInBotChannel { get; }

		/// <summary>
		/// This <see cref="BotContext"/>'s <see cref="DataPersistence"/>.
		/// </summary>
		public DataPersistence Storage { get; }

		/// <summary>
		/// Storage just for permissions
		/// </summary>
		private DataPersistence PermissionStorage { get; }

		/// <summary>
		/// A method that runs after the context's guild downloads.
		/// </summary>
		/// <returns></returns>
		public virtual Task AfterContextInitialization() {
			return Task.CompletedTask;
		}

		public BotContext() {
			Storage = DataPersistence.GetPersistence(this);
			PermissionStorage = DataPersistence.GetPersistence(this, "permissions.cfg");

			foreach (string idStr in PermissionStorage.Keys) {
				if (Snowflake.TryParse(idStr, out Snowflake userId) && byte.TryParse(PermissionStorage.GetValue(idStr), out byte pLvl)) {
					PermissionRegistry[userId] = (PermissionLevel)pLvl;
				}
			}
		}

		internal async Task OnGuildCreated(Guild guild) {
			ContextLogger.WriteLine($"Guild created event fired for this guild.", LogLevel.Info);
			if (Server != null) {
				ContextLogger.WriteLine($"...But this context is already registered, so the event has been discarded.", LogLevel.Info);
				return; // Ignore this.
			}

			if (guild.ID != ServerID) return;
			Server = guild;

			ContextLogger.WriteLine("Guild created. Performing extra tasks...", LogLevel.Info);

			if (DownloadsAllMembers) {
				ContextLogger.WriteLine("Downloading all guild members (DownloadsAllMembers=true)", LogLevel.Info);
				await Server.DownloadAllMembersAsync().ConfigureAwait(false);
				ContextLogger.WriteLine($"Done! Downloaded {Server.Members.Count} members (of the {Server.MaxMembers} members that this server is authorized to contain).");
			}

			ContextLogger.WriteLine("Calling AfterContextInitialization...", LogLevel.Info);

			await AfterContextInitialization();

			ContextLogger.WriteLine("Done.", LogLevel.Info);
		}

		/// <summary>
		/// Returns the registered permissions of the given member.
		/// </summary>
		/// <param name="member"></param>
		/// <exception cref="ArgumentNullException">If the member is null.</exception>
		public PermissionLevel GetPermissionsOf(Member member) {
			if (member == null) throw new ArgumentNullException(nameof(member));
			if (member.IsSelf) return PermissionLevel.Bot;
			return PermissionRegistry.GetValueOrDefault(member.ID, PermissionLevel.StandardUser);
		}

		/// <summary>
		/// Sets the permissions of the given member to the given value. This is persistent.
		/// </summary>
		/// <param name="member"></param>
		/// <param name="newLevel"></param>
		/// <exception cref="ArgumentNullException">If the member is null.</exception>
		/// <exception cref="InvalidOperationException">If the member is the bot and the new level isn't <see cref="PermissionLevel.Bot"/></exception>
		public void SetPermissionsOf(Member member, PermissionLevel newLevel) {
			if (member == null) throw new ArgumentNullException(nameof(member));
			if (member.IsSelf && newLevel != PermissionLevel.Bot) throw new InvalidOperationException(Personality.Get("err.perms.changebot"));
			if (GetPermissionsOf(member) == newLevel) return;
			PermissionRegistry[member.ID] = newLevel;

			foreach (KeyValuePair<Snowflake, PermissionLevel> permInfo in PermissionRegistry) {
				if (permInfo.Value == PermissionLevel.StandardUser) {
					// Don't write permission level 2, just remove it.
					PermissionStorage.RemoveValue(permInfo.Key.ToString());
					continue;
				}
				PermissionStorage.SetValue(permInfo.Key.ToString(), (int)permInfo.Value, true);
			}
			PermissionStorage.Save();
			
		}

		/// <summary>
		/// Returns the first instance of <typeparamref name="T"/> in this <see cref="BotContext"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetPassiveHandlerInstance<T>() where T : PassiveHandler {
			return (T)Handlers.FirstOrDefault(handler => handler.GetType() == typeof(T));
		}

		/// <summary>
		/// Returns the first instance of a <see cref="PassiveHandler"/> whose name starts with <paramref name="name"/>, case insensitive in this <see cref="BotContext"/>.
		/// </summary>
		/// <returns></returns>
		public PassiveHandler FindPassiveHandlerInstance(string name)  {
			return Handlers.FirstOrDefault(handler => handler.Name.ToLower().StartsWith(name.ToLower()));
		}

		public override bool Equals(object obj) {
			if (obj is BotContext other) return Equals(other);
			return ReferenceEquals(this, obj);
		}

		public bool Equals([AllowNull] BotContext other) {
			if (other is null) return false;
			return ReferenceEquals(this, other) || ID == other.ID;
		}

		public static bool operator ==([AllowNull] BotContext left, [AllowNull] BotContext right) {
			if (left is null && right is null) return true;
			if (left is null || right is null) return false;
			return left.Equals(right);
		}

		public static bool operator !=([AllowNull] BotContext left, [AllowNull] BotContext right) => !(left == right);

		public override int GetHashCode() {
			return HashCode.Combine(ID);
		}

	}
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;
using EtiLogger.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static EtiBotCore.DiscordObjects.Universal.Data.PermissionInformation;

namespace EtiBotCore.DiscordObjects.Base {

	/// <summary>
	/// Represents a channel that is in a server.
	/// </summary>
	
	public abstract class GuildChannelBase : ChannelBase, IComparable<GuildChannelBase> {

		internal static readonly ThreadedDictionary<Snowflake, GuildChannelBase> InstantiatedChannelsByID = new ThreadedDictionary<Snowflake, GuildChannelBase>();

		/// <summary>
		/// The ID of the server that this channel exists in.
		/// </summary>
		public Snowflake ServerID { get; }

		/// <summary>
		/// A reference to the actual server that this channel exists in.
		/// </summary>
		public Guild Server { get; }

		/// <summary>
		/// The name of this channel.
		/// </summary>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the permissions needed to do this.</exception>
		/// <exception cref="ObjectDeletedException">If this object has been deleted and cannot be edited.</exception>
		/// <exception cref="ArgumentNullException">If the name is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the name contains invalid characters, is shorter than 2 characters, or is longer than 100 characters.</exception>
		public virtual string Name {
			get => _Name;
			set {
				if (value == null) throw new ArgumentNullException(nameof(value));
				int minLength = 1;
				Permissions requiredPerms;
				if (!(this is Thread)) {
					if (value.Contains(' ')) throw new ArgumentOutOfRangeException(nameof(value), "Invalid characters in channel name!");
					minLength = 2;
					requiredPerms = Payloads.Data.Permissions.ManageChannels;
				} else {
					requiredPerms = Payloads.Data.Permissions.ManageThreads;
				}
				
				if (value.Length < minLength || value.Length > 100) throw new ArgumentOutOfRangeException(nameof(value), "The name is too short or long!");
				EnforcePermissions(Server!, requiredPerms);
				SetProperty(ref _Name, value);
			}
		}
		private string _Name = string.Empty;

		/// <summary>
		/// The ID of the parent category. This can be set in tandem with <see cref="Position"/> to modify its position in this specific category rather than in the entire channel list.<para/>
		/// If this is set after <see cref="Position"/>, and <see cref="Position"/> is a value beyond the number of channels in this category, then it will be clamped to the end of the category.<para/>
		/// Raises <see cref="InvalidOperationException"/> if referenced on a <see cref="Thread"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">If this is referenced in any way on a <see cref="Thread"/>.</exception>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the permissions needed to edit this.</exception>
		/// <exception cref="ObjectDeletedException">If this object has been deleted and cannot be edited.</exception>
		public ChannelCategory? ParentCategory {
			get {
				if (Type.IsThreadChannel()) {
					throw new InvalidOperationException("Cannot get the parent category of a thread, as they have a channel as a parent, not a category!");
				}
				if (_ParentCategory == null && ParentID != null) {
					_ParentCategory = GetFromCache<ChannelCategory>(ParentID.Value);
				}
				return _ParentCategory;
			}
			set {
				if (Type.IsThreadChannel()) {
					throw new InvalidOperationException("Cannot set the parent category of a thread, as they have a channel as a parent, not a category!");
				}
				EnforcePermissions(Server!, Payloads.Data.Permissions.ManageChannels);
				SetProperty(ref _ParentCategory, value);
				ParentID = value?.ID;
			}
		}
		internal ChannelCategory? _ParentCategory = null;

		/// <summary>
		/// The ID of this channel's parent. For traditional guild channels, this will be the ID of their parent category 
		/// (or null if this channel is not in a category), and for threads, this is the ID of the channel that this thread is a part of.
		/// </summary>
		public Snowflake? ParentID { get; protected set; }

		/// <summary>
		/// The permissions that apply to this channel. If this is a thread, then this references the permissions of the parent channel (see <see cref="Thread.ParentChannel"/>).
		/// </summary>
		/// <remarks>
		/// <strong>This reference is cloned in clone objects.</strong>
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the permissions needed to edit this.</exception>
		/// <exception cref="ObjectDeletedException">If this object has been deleted and cannot be edited.</exception>
		public virtual PermissionContainer Permissions {
			get {
				if (PermsCache == null) {
					PermsCache = new PermissionContainer(this);
				}
				return PermsCache;
			}
		}
		private PermissionContainer? PermsCache = null;

		/// <summary>
		/// The position of this channel in the list. If <see cref="ParentCategory"/> is set as well, then this will be the position relative to the category (where 0 will put it as the first channel, and <c>n</c> will put it as the last).<para/>
		/// Raises <see cref="InvalidOperationException"/> if referenced on a <see cref="Thread"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">If this is referenced in any way on a <see cref="Thread"/>.</exception>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the permissions needed to do this.</exception>
		/// <exception cref="ObjectDeletedException">If this object has been deleted and cannot be edited.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the new value is less than zero or greater than the number of channels in the server, or if <see cref="ParentCategory"/> is set, greater than the number of channels in this category.</exception>
		public int Position {
			get {
				if (Type.IsThreadChannel()) {
					throw new InvalidOperationException("Threads do not have a position!");
				}
				return _Position;
			}
			set {
				if (Type.IsThreadChannel()) {
					throw new InvalidOperationException("Threads do not have a position!");
				}
				if (value < 0 || value > Server!.Channels.Count) throw new ArgumentOutOfRangeException("Position cannot be less than zero or greater than the number of channels in the server!");
				EnforcePermissions(Server, Payloads.Data.Permissions.ManageChannels);
				if (HasChange("ParentCategory") && ParentCategory != null) {
					// If we changed the parent category and set it to something before we changed this, then we make this relative to the category.

					int catBasePos = 0; // The position that will be set to make this the first channel in the category.
					int maxPos = 0; // The position just before the next category.
					foreach (ChannelCategory category in Server.ChannelCategories) {
						if (category.Position > ParentCategory!.Position) {
							catBasePos = ParentCategory!.Position + 1;
							maxPos = category.Position;
						}
					}
					if (value > (maxPos - catBasePos + 1)) {
						// Add +1 buffer room so that they can add it to the end of the category.
						throw new ArgumentOutOfRangeException("The position cannot be set to " + value + " when the target category is set to " + ParentCategory!.Name + " because it only contains channels in the range of " + catBasePos + " and " + maxPos + ".");
					}
				}
				SetProperty(ref _Position, value);
			}
		}
		private int _Position = 0;

		/// <summary>
		/// Instantiates a new <see cref="GuildChannelBase"/> from the given payload, guild, and channel type.
		/// </summary>
		/// <param name="payload"></param>
		/// <param name="guild"></param>
		/// <param name="type"></param>
		internal GuildChannelBase(Payloads.PayloadObjects.Channel payload, Guild guild, ChannelType type) : base(payload.ID, type) {
			ServerID = payload.GuildID!.Value;
			Server = guild;
			InstantiatedChannelsByID[payload.ID] = this;
		}

		/// <summary>
		/// Returns a channel from cache that has the given ID, or <see langword="null"/> if it was not instantiated yet (in which case you should call <see cref="GetOrCreateAsync{T}(Payloads.PayloadObjects.Channel, Guild?)"/>)
		/// </summary>
		/// <typeparam name="T">A type of guild channel, or <see cref="GuildChannelBase"/> itself.</typeparam>
		/// <param name="ID"></param>
		/// <returns></returns>
		internal static T? GetFromCache<T>(Snowflake ID) where T : GuildChannelBase {
			if (InstantiatedChannelsByID.TryGetValue(ID, out GuildChannelBase? channel)) {
				return (T)channel;
			}
			return null;
		}

		/// <summary>
		/// Gets an existing channel or creates a new one of the given type.
		/// </summary>
		/// <typeparam name="T">The type of channel to return. If this is <see cref="GuildChannelBase"/>, it will automatically try to figure out what type to return based on the type in the payload.</typeparam>
		/// <param name="plChannel"></param>
		/// <param name="server"></param>
		/// <returns></returns>
		internal static async Task<T> GetOrCreateAsync<T>(Payloads.PayloadObjects.Channel plChannel, Guild? server = null) where T : GuildChannelBase {
			T? existing = GetFromCache<T>(plChannel.ID);
			if (existing != null) return existing;
			if (plChannel.GuildID == null) throw new ArgumentException("Payload's guild ID was null! Is this a guild channel?");

			// Doesn't exist.
			Type targetType = typeof(T);
			Type baseType = typeof(GuildChannelBase);
			GuildChannelBase channel;

			server ??= await Guild.GetOrDownloadAsync(plChannel.GuildID.Value, true);

			if (plChannel.Type == ChannelType.Text || plChannel.Type == ChannelType.Store || plChannel.Type == ChannelType.News) {
				if (targetType != baseType && targetType != typeof(TextChannel)) {
					throw new InvalidCastException($"{nameof(GetOrCreateAsync)} was called with a type parameter of {targetType.Name}, but the channel payload stated the channel was actually a {plChannel.Type} instance!");
				}
				//Logger.Default.WriteLine($"Created a new TextChannel ID={plChannel.ID}");
				channel = new TextChannel(plChannel, server);
			} else if (plChannel.Type == ChannelType.Voice) {
				if (targetType != baseType && targetType != typeof(VoiceChannel)) {
					throw new InvalidCastException($"{nameof(GetOrCreateAsync)} was called with a type parameter of {targetType.Name}, but the channel payload stated the channel was actually a {plChannel.Type} instance!");
				}
				//Logger.Default.WriteLine($"Created a new VoiceChannel ID={plChannel.ID}");
				channel = new VoiceChannel(plChannel, server);
			} else if (plChannel.Type == ChannelType.Category) {
				if (targetType != baseType && targetType != typeof(ChannelCategory)) {
					throw new InvalidCastException($"{nameof(GetOrCreateAsync)} was called with a type parameter of {targetType.Name}, but the channel payload stated the channel was actually a {plChannel.Type} instance!");
				}
				//Logger.Default.WriteLine($"Created a new ChannelCategory ID={plChannel.ID}");
				channel = new ChannelCategory(plChannel, server);
			} else if (plChannel.Type.IsThreadChannel()) {
				if (targetType != baseType && targetType != typeof(Thread)) {
					throw new InvalidCastException($"{nameof(GetOrCreateAsync)} was called with a type parameter of {targetType.Name}, but the channel payload stated the channel was actually a {plChannel.Type} instance!");
				}
				//Logger.Default.WriteLine($"Created a new Thread ID={plChannel.ID}");
				channel = new Thread(plChannel, GetFromCache<TextChannel>(plChannel.ParentID!.Value)!, plChannel.Type);
			} else {
				throw new InvalidOperationException("Unknown channel type!");
			}

			InstantiatedChannelsByID[plChannel.ID] = channel;
			return (T)channel;
		}

		/// <inheritdoc/>
		protected internal override async Task Update(PayloadDataObject obj, bool skipNonNullFields = false) {
			await base.Update(obj, skipNonNullFields);
			if (obj is Payloads.PayloadObjects.Channel channel) {
				_Name = AppropriateValue(Name, channel.Name!, skipNonNullFields);
				_Position = channel.Position.GetValueOrDefault(); // Guild channel, will exist
				ParentID = channel.ParentID;
				_ParentCategory = null; // reset
				LastMessageID = channel.LastMessageID;
				LastPinTimestamp = channel.LastPinTimestamp;
				var perms = channel.PermissionOverwrites;
				if (perms != null) {
					foreach (var perm in perms) {
						// Locked will not be true here because there's a check for it before this method is called.
						PermissionInformation permsForThing = Permissions.GetOrRegisterForDataPopulation(perm.ID, true)!;
						permsForThing.DataReset();
						permsForThing.SetTo(perm.AllowPermissions, PermissionState.Allow, true);
						permsForThing.SetTo(perm.DenyPermissions, PermissionState.Deny, true);
					}
				}
			}
		}

		/// <inheritdoc/>
		protected override Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changes, string? reasons) {
			// Don't use me.
			return base.SendChangesToDiscord(changes, reasons);
		}

		/// <inheritdoc/>
		protected override Task<APIRequestData> SendChangesToDiscordCustom(IReadOnlyDictionary<string, object> changes, string? reasons) {
			return Task.Run(async () => {
				APIRequestData request = await base.SendChangesToDiscordCustom(changes, reasons);

				if (changes.ContainsKey("Name")) request.SetJsonField("name", Name);

				int basePos = 0;
				if (changes.ContainsKey("ParentCategory")) {
					if (ParentCategory != null) {
						basePos = ParentCategory.Position + 1;
					}
					request.SetJsonField("parent_id", ParentCategory?.ID);
				}
				if (changes.ContainsKey("Position")) {
					request.SetJsonField("position", basePos + Position);
				}
				if (changes.ContainsKey("StoredPermissions")) request.SetJsonField("permission_overwrites", Permissions.ToJson());

				return request;
			});
		}

		/// <summary>
		/// Sorts this channel relative to another by its <see cref="Position"/>.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo([AllowNull] GuildChannelBase other) {
			if (other is null) return 1;
			return other.Position - Position;
		}

		/// <inheritdoc/>
		public override DiscordObject MemberwiseClone() {
			GuildChannelBase channel = (GuildChannelBase)base.MemberwiseClone();
			channel.PermsCache = Permissions.Clone();
			return channel;
		}
	}
}

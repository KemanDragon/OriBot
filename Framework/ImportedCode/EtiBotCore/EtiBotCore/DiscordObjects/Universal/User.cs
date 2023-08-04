using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.Exceptions;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;
using static EtiBotCore.Data.Constants;

namespace EtiBotCore.DiscordObjects.Universal {

	/// <summary>
	/// Represents a generic user, not in any particular server -- just the person themselves.
	/// </summary>
	
	public class User : DiscordObject {

		internal static readonly ThreadedDictionary<Snowflake, User> InstantiatedUsers = new ThreadedDictionary<Snowflake, User>();

		#region Web Requests

		internal static readonly SendableAPIRequestFactory GetCurrentUser = new SendableAPIRequestFactory("users/@me", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetUser = new SendableAPIRequestFactory("users/{0}", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory ModifyCurrentUser = new SendableAPIRequestFactory("users/@me", SendableAPIRequestFactory.HttpRequestType.Patch);

		internal static readonly SendableAPIRequestFactory GetCurrentUserGuilds = new SendableAPIRequestFactory("users/@me/guilds", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory LeaveGuild = new SendableAPIRequestFactory("users/@me/guilds/{0}", SendableAPIRequestFactory.HttpRequestType.Delete);

		internal static readonly SendableAPIRequestFactory GetUserDMs = new SendableAPIRequestFactory("users/@me/channels", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory CreateDM = new SendableAPIRequestFactory("users/@me/channels", SendableAPIRequestFactory.HttpRequestType.Post);

		internal static readonly SendableAPIRequestFactory GetUserConnections = new SendableAPIRequestFactory("users/@me/connections", SendableAPIRequestFactory.HttpRequestType.Get);

		#endregion

		#region Properties

		/// <summary>
		/// A mention to this user by ID. This string pings them if sent in a chat message.
		/// </summary>
		public string Mention => $"<@!{ID}>";

		/// <summary>
		/// A reference to the Discord user representing this bot.
		/// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public static User BotUser { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

		/// <summary>
		/// Whether or not the current user is equal to <see cref="BotUser"/>.
		/// </summary>
		public bool IsSelf {
			get {
				if (_IsSelf == null) {
					_IsSelf = ID == BotUser.ID;
				}
				return _IsSelf.Value;
			}
		}
		private bool? _IsSelf = null;

		/// <summary>
		/// The URL to this user's avatar, or their Discord-assigned default avatar if they don't have one set.
		/// </summary>
		/// <remarks>
		/// <strong>This reference is cloned in clone objects.</strong>
		/// </remarks>
		public Uri AvatarURL => AvatarHash != null ? HashToUriConverter.GetUserAvatar(ID, AvatarHash)! : HashToUriConverter.GetUserDefaultAvatar(int.Parse(Discriminator));

		/// <summary>
		/// The has to this user's avatar, or <see langword="null"/> if they don't have done.
		/// </summary>
		public string? AvatarHash { get; protected set; }

		/// <summary>
		/// This user's discriminiator, which is the four digits after their username, e.g. <c>1760</c> in <c>Eti#1760</c>.
		/// </summary>
		public string Discriminator { get; protected set; } = "0000";

		/// <summary>
		/// The user's username.
		/// </summary>
		public string Username { get; protected set; } = string.Empty;

		/// <summary>
		/// This user's full name, which is their username#discriminator e.g. <c>Eti#1760</c>.
		/// </summary>
		public string FullName => Username + "#" + Discriminator;

		/// <summary>
		/// Whether or not this user is a bot. To check if this user is <em>this</em> bot, use <see cref="IsSelf"/>.
		/// </summary>
		public bool IsABot { get; protected set; } = false;

		/// <summary>
		/// Whether or not this user is a representation of Discord's System, which is used to relay incredibly important messages.
		/// </summary>
		public bool IsDiscordSystem { get; protected set; } = false;

		/// <summary>
		/// This user's locale.
		/// </summary>
		public string Locale { get; protected set; } = "en-US";

		/// <summary>
		/// Whether or not this user has enabled 2 factor authentication on their account.
		/// </summary>
		public bool Has2FA { get; protected set; } = false;

		/// <summary>
		/// This user's email, or <see langword="null"/> if the bot cannot access this data.
		/// </summary>
		public string? Email { get; protected set; }

		/// <summary>
		/// Whether or not this user's email has been verified, or <see langword="null"/> if the bot cannot access this data.<para/>
		/// It is only possible to acquire this data through an application.
		/// </summary>
		public bool? EmailVerified { get; protected set; }

		/// <summary>
		/// The type of Nitro subscription this user has.
		/// </summary>
		public PremiumType NitroType { get; set; }

		/// <summary>
		/// The attributes this user has.
		/// </summary>
		public UserFlags Flags { get; set; }

		/// <summary>
		/// The creation date of this user's Discord account (UTC+0) acquired from their <see cref="DiscordObject.ID"/>.
		/// </summary>
		public DateTimeOffset AccountCreationDate => ID.ToDateTimeOffset();

		/// <summary>
		/// The age of this user's Discord account.
		/// </summary>
		public TimeSpan AccountAge => DateTimeOffset.UtcNow - AccountCreationDate;

		/// <summary>
		/// The current status of this user in a voice channel. Always check <see cref="VoiceState.IsConnectedToVoice"/> before referencing other properties, which may be their <see langword="default"/>s if the user is not connected.
		/// </summary>
		/// <remarks>
		/// <strong>This reference is cloned in clone objects.</strong>
		/// </remarks>
		public VoiceState VoiceState {
			get {
				if (_VoiceState == null) {
					_VoiceState = VoiceState.GetStateForOrCreate(ID);
				}
				return _VoiceState;
			}
		}
		private VoiceState? _VoiceState = null;

		#endregion

		#region Extended Stuffs

		/// <summary>
		/// Attempts to create a DM channel with this user. Returns a <see cref="DMChannel"/> if successful, and <see langword="null"/> if the user has DMs off.
		/// </summary>
		/// <returns></returns>
		public async Task<DMChannel?> TryCreateDMAsync() {
			(var dmc, _) = await CreateDM.ExecuteAsync<Payloads.PayloadObjects.Channel>(new APIRequestData().SetJsonField("recipient_id", ID));
			if (dmc == null) {
				// dinty did not plae deeemcee2.....,
				return null;
			}
			return await DMChannel.GetOrCreateAsync(dmc);
		}

		/// <summary>
		/// Attempts to create a DM channel with this user, and if it is successful, sends the given message. Returns a <see cref="Message"/> representing the sent message if successful, or <see langword="null"/> if the user could not be DMed.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="embed"></param>
		/// <returns></returns>
		public async Task<Message?> TrySendDMAsync(string? content = null, Embed? embed = null) {
			if (string.IsNullOrWhiteSpace(content) && embed == null) throw new ArgumentNullException("content and embed");
			DMChannel? channel = await TryCreateDMAsync();
			if (channel == null) return null;
			return await channel.SendMessageAsync(content, embed);
		}

		/// <summary>
		/// Returns this <see cref="User"/> as a <see cref="Member"/> in a particular server. This may need to download the member, and will return <see langword="null"/> if the user is not in this server.
		/// </summary>
		/// <param name="server">The server to get this member from.</param>
		/// <returns></returns>
		public async Task<Member?> InServerAsync(Guild server) {
			if (this is Member member && member.Server == server) return member; // This is already a member object in the given server. Just give it back.
			return await Member.GetOrCreateAsync(ID, server);
		}

		#endregion

		/// <summary>
		/// Construct a new user instance from the given payload. Always try <see cref="GetOrDownloadUserAsync"/> first. 
		/// </summary>
		/// <param name="user"></param>
		internal User(Payloads.PayloadObjects.User user) : base(user.UserID) {
			InstantiatedUsers[user.UserID] = this;
			Update(user, false);
		}

		/// <summary>
		/// Provides an existing User object, or downloads it if one with this ID has not already been created. Returns <see langword="null"/> if the ID is malformed or does not correspond to a user.
		/// </summary>
		/// <param name="id">The ID of this user.</param>
		/// <returns></returns>
		public static async Task<User?> GetOrDownloadUserAsync(Snowflake id) {
			bool create = !InstantiatedUsers.ContainsKey(id);
			User user;
			if (create) {
				(var plUser, var req) = await GetUser.ExecuteAsync<Payloads.PayloadObjects.User>(new APIRequestData { Params = { id } });
				if (!req!.IsSuccessStatusCode) return null;
				user = new User(plUser!);
				InstantiatedUsers[id] = user;
			} else {
				user = InstantiatedUsers[id];
			}
			return user;
		}

		/// <summary>
		/// Creates a new <see cref="User"/> or gets an existing one by ID. This does not download the user like <see cref="GetOrDownloadUserAsync(Snowflake)"/>
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		internal static User EventGetOrCreate(Payloads.PayloadObjects.User user) {
			if (!InstantiatedUsers.ContainsKey(user.UserID)) {
				InstantiatedUsers[user.UserID] = new User(user);
			}
			return InstantiatedUsers[user.UserID];
		}

		internal static bool UserExists(Snowflake id) => InstantiatedUsers.ContainsKey(id);

		/// <inheritdoc/>
		protected internal override Task Update(PayloadDataObject obj, bool skipNonNullFields) {
			if (obj is Payloads.PayloadObjects.User userPayload) {
				// ID = userPayload.UserID;
				Username = AppropriateValue(Username, userPayload.Username, skipNonNullFields);
				Discriminator = AppropriateValue(Discriminator, userPayload.Discriminator, skipNonNullFields);
				IsABot = AppropriateNullableValue(IsABot, userPayload.IsBot, skipNonNullFields);
				IsDiscordSystem = AppropriateValue(IsDiscordSystem, userPayload.IsSystem ?? false, skipNonNullFields);

				Locale = AppropriateNullableValue(Locale, userPayload.Locale, skipNonNullFields);
				Has2FA = AppropriateNullableValue(Has2FA, userPayload.MFAEnabled, skipNonNullFields);

				AvatarHash = userPayload.AvatarHash == UNSENT_STRING_DEFAULT ? AvatarHash : userPayload.AvatarHash;

				Email = AppropriateValue(Email, userPayload.Email, skipNonNullFields);
				EmailVerified = AppropriateValue(EmailVerified, userPayload.EmailVerified, skipNonNullFields);

				NitroType = AppropriateNullableValue(NitroType, userPayload.PremiumType, skipNonNullFields);
				Flags = AppropriateNullableValue(Flags, userPayload.Flags, skipNonNullFields);
			}
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		protected override Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changesAndOriginalValues, string? reason) => Task.FromResult<HttpResponseMessage?>(null);

		// Nothing to send to Discord. Normally you would call this in Member but you have removed it.

		/// <summary>
		/// Sets <see cref="BotUser"/>.
		/// </summary>
		/// <returns></returns>
		public static async Task SetupSelfUser() {
			// ^ Set up from DiscordClient
			(var plUser, _) = await GetCurrentUser.ExecuteAsync<Payloads.PayloadObjects.User>();
			BotUser = EventGetOrCreate(plUser!);
		}

		/// <inheritdoc/>
		public override DiscordObject MemberwiseClone() {
			User newUser = (User)base.MemberwiseClone();
			newUser._VoiceState = VoiceState.MemberwiseClone(newUser);
			return newUser;
		}
	}
}

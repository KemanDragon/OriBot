using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Client.EventContainers;
using EtiBotCore.Clockwork;
using EtiBotCore.Data.Container;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.AuditLog;
using EtiBotCore.DiscordObjects.Guilds.MemberData;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Exceptions;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.Events;
using EtiBotCore.Payloads.Events.Intents.Guilds;
using EtiBotCore.Utility;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static EtiBotCore.Data.Constants;

namespace EtiBotCore.DiscordObjects.Universal {

	/// <summary>
	/// Represents a guild, better known as a "server" to most. This contains all necessary properties and methods required to interract with this guild.
	/// </summary>
	
	public class Guild : DiscordObject {

		/// <summary>
		/// All guild objects that have been instantiated.
		/// </summary>
		internal static readonly ThreadedDictionary<Snowflake, Guild> InstantiatedGuilds = new ThreadedDictionary<Snowflake, Guild>();

		#region HTTP Requests

		internal static readonly SendableAPIRequestFactory CreateGuild = new SendableAPIRequestFactory("guilds", SendableAPIRequestFactory.HttpRequestType.Post);

		/// <summary>
		/// Parameters: <c>guildID</c>
		/// </summary>
		internal static readonly SendableAPIRequestFactory GetGuild = new SendableAPIRequestFactory("guilds/{0}", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetGuildPreview = new SendableAPIRequestFactory("guilds/{0}/preview", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory ModifyGuild = new SendableAPIRequestFactory("guilds/{0}", SendableAPIRequestFactory.HttpRequestType.Patch);

		internal static readonly SendableAPIRequestFactory DeleteGuild = new SendableAPIRequestFactory("guilds/{0}", SendableAPIRequestFactory.HttpRequestType.Delete);

		internal static readonly SendableAPIRequestFactory GetGuildChannels = new SendableAPIRequestFactory("guilds/{0}/channels", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory CreateGuildChannel = new SendableAPIRequestFactory("guilds/{0}/channels", SendableAPIRequestFactory.HttpRequestType.Post);

		internal static readonly SendableAPIRequestFactory ModifyGuildChannelPosition = new SendableAPIRequestFactory("guilds/{0}/channels", SendableAPIRequestFactory.HttpRequestType.Patch);

		internal static readonly SendableAPIRequestFactory GetGuildMember = new SendableAPIRequestFactory("guilds/{0}/members/{1}", SendableAPIRequestFactory.HttpRequestType.Get) {
			SpecialErrorRemarks = {
				[404] = null // 404 is thrown if the member left the server.
			}
		};

		internal static readonly SendableAPIRequestFactory ListGuildMembers = new SendableAPIRequestFactory("guilds/{0}/members", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory AddGuildMember = new SendableAPIRequestFactory("guilds/{0}/members/{1}", SendableAPIRequestFactory.HttpRequestType.Put);

		internal static readonly SendableAPIRequestFactory ModifyGuildMember = new SendableAPIRequestFactory("guilds/{0}/members/{1}", SendableAPIRequestFactory.HttpRequestType.Patch) {
			SpecialErrorRemarks = {
				[403] = "§4Is this member higher ranking than the bot / the owner of the server? If so, the bot will not be able to modify them."
			}
		};

		internal static readonly SendableAPIRequestFactory ModifyCurrentUserNick = new SendableAPIRequestFactory("guilds/{0}/members/@me/nick", SendableAPIRequestFactory.HttpRequestType.Patch);

		internal static readonly SendableAPIRequestFactory AddGuildMemberRole = new SendableAPIRequestFactory("guilds/{0}/members/{1}/roles/{2}", SendableAPIRequestFactory.HttpRequestType.Put);

		internal static readonly SendableAPIRequestFactory RemoveGuildMemberRole = new SendableAPIRequestFactory("guilds/{0}/members/{1}/roles/{2}", SendableAPIRequestFactory.HttpRequestType.Delete);

		internal static readonly SendableAPIRequestFactory RemoveGuildMember = new SendableAPIRequestFactory("guilds/{0}/members/{1}", SendableAPIRequestFactory.HttpRequestType.Delete);

		internal static readonly SendableAPIRequestFactory GetGuildBans = new SendableAPIRequestFactory("guilds/{0}/bans", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetGuildBan = new SendableAPIRequestFactory("guilds/{0}/bans/{1}", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory CreateGuildBan = new SendableAPIRequestFactory("guilds/{0}/bans/{1}", SendableAPIRequestFactory.HttpRequestType.Put);

		internal static readonly SendableAPIRequestFactory RemoveGuildBan = new SendableAPIRequestFactory("guilds/{0}/bans/{1}", SendableAPIRequestFactory.HttpRequestType.Delete);

		internal static readonly SendableAPIRequestFactory GetGuildRoles = new SendableAPIRequestFactory("guilds/{0}/roles", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory CreateGuildRole = new SendableAPIRequestFactory("guilds/{0}/roles", SendableAPIRequestFactory.HttpRequestType.Post);

		internal static readonly SendableAPIRequestFactory ModifyGuildRolePosition = new SendableAPIRequestFactory("guilds/{0}/roles", SendableAPIRequestFactory.HttpRequestType.Patch);

		internal static readonly SendableAPIRequestFactory ModifyGuildRole = new SendableAPIRequestFactory("guilds/{0}/roles/{1}", SendableAPIRequestFactory.HttpRequestType.Patch);

		internal static readonly SendableAPIRequestFactory DeleteGuildRole = new SendableAPIRequestFactory("guilds/{0}/roles/{1}", SendableAPIRequestFactory.HttpRequestType.Delete);

		internal static readonly SendableAPIRequestFactory GetGuildPruneCount = new SendableAPIRequestFactory("guilds/{0}/prune", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory BeginGuildPrune = new SendableAPIRequestFactory("guilds/{0}/prune", SendableAPIRequestFactory.HttpRequestType.Post);

		internal static readonly SendableAPIRequestFactory GetGuildVoiceRegions = new SendableAPIRequestFactory("guilds/{0}/regions", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetGuildInvites = new SendableAPIRequestFactory("guilds/{0}/invites", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetGuildIntegrations = new SendableAPIRequestFactory("guilds/{0}/integrations", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory CreateGuildIntegration = new SendableAPIRequestFactory("guilds/{0}/integrations", SendableAPIRequestFactory.HttpRequestType.Post);

		internal static readonly SendableAPIRequestFactory ModifyGuildIntegration = new SendableAPIRequestFactory("guilds/{0}/integrations/{1}", SendableAPIRequestFactory.HttpRequestType.Patch);

		internal static readonly SendableAPIRequestFactory DeleteGuildIntegration = new SendableAPIRequestFactory("guilds/{0}/integrations/{1}", SendableAPIRequestFactory.HttpRequestType.Delete);

		internal static readonly SendableAPIRequestFactory SyncGuildIntegration = new SendableAPIRequestFactory("guilds/{0}/integrations/{1}/sync", SendableAPIRequestFactory.HttpRequestType.Post);

		internal static readonly SendableAPIRequestFactory GetGuildWidgetSettings = new SendableAPIRequestFactory("guilds/{0}/widget", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory ModifyGuildWidget = new SendableAPIRequestFactory("guilds/{0}/widget", SendableAPIRequestFactory.HttpRequestType.Patch);

		internal static readonly SendableAPIRequestFactory GetGuildWidget = new SendableAPIRequestFactory("guilds/{0}/widget.json", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetGuildVanityUrl = new SendableAPIRequestFactory("guilds/{0}/vanity-url", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetGuildWidgetImage = new SendableAPIRequestFactory("guilds/{0}/widget.png", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetAuditLog = new SendableAPIRequestFactory("guilds/{0}/audit-logs", SendableAPIRequestFactory.HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetServerEmojis = new SendableAPIRequestFactory("guilds/{0}/emojis", SendableAPIRequestFactory.HttpRequestType.Get);

		#endregion

		#region Main Guild Properties

		#region Display Info & Appearance

		/// <summary>
		/// The name of this server.
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public string Name {
			get => _Name;
			set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _Name, value);
			}
		}
		private string _Name = string.Empty;

		/// <summary>
		/// The icon of this server.
		/// </summary>
		public Uri? ServerIcon => ServerIconHash != null && ID != null ? HashToUriConverter.GetGuildIcon(ID.Value, ServerIconHash) : null;

		/// <summary>
		/// The has to the icon of this server.
		/// </summary>
		public string? ServerIconHash { get; private set; }

		/// <summary>
		/// The banner of this server, if applicable.
		/// </summary>
		public Uri? BannerImageURL => BannerHash != null && ID != null ? HashToUriConverter.GetGuildBanner(ID.Value, BannerHash) : null;

		/// <summary>
		/// The hash to the banner of this server.
		/// </summary>
		public string? BannerHash { get; private set; }

		#endregion

		#region Publicity & Discovery

		/// <summary>
		/// The vanity URL code of the server, or <see langword="null"/> if it does not have one. Cannot be changed by bots.
		/// </summary>
		public string? VanityURLCode { get; private set; }

		/// <summary>
		/// The ID of the voice region for this server.
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public string VoiceRegion {
			get => _VoiceRegion;
			set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _VoiceRegion, value);
			}
		}
		internal string _VoiceRegion = string.Empty;

		#region Server Discovery

		/// <summary>
		/// The description of this server, which only applies to discoverable servers. If the server does not have one, this will be <see langword="null"/>.
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		public string? Description { get; private set; }

		/// <summary>
		/// The discovery page image, if applicable. If the server does not have one, this will be <see langword="null"/>.
		/// </summary>
		public Uri? DiscoverySplashURL => DiscoverySplashHash != null ? HashToUriConverter.GetGuildDiscoverySplash(ID, DiscoverySplashHash) : null;

		/// <summary>
		/// The hash of the discovery splash, if applicable. If the server does not have one, this will be <see langword="null"/>.
		/// </summary>
		public string? DiscoverySplashHash { get; private set; }

		/// <summary>
		/// The preferred locale of this server if it's public. Defaults to "en-US".
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public string PreferredLocale {
			get => _PreferredLocale;
			set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _PreferredLocale, value);
			}
		}
		private string _PreferredLocale = "en-US";

		#endregion

		#region Nitro

		/// <summary>
		/// The amount of people boosting this server.
		/// </summary>
		public int NitroBoosterCount { get; private set; }

		/// <summary>
		/// The tier of the nitro boost on this server.
		/// </summary>
		public PremiumTier NitroBoostTier { get; private set; } = PremiumTier.None;

		#endregion

		#region Discord Interaction

		/// <summary>
		/// The public updates channel.<para/>
		/// <strong>Only available to Community Servers</strong>
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="GuildFeatureNotAvailableException">If this guild is not registered as a public server, and does not have nor need a public updates channel.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public TextChannel? PublicUpdatesChannel {
			get => _PublicUpdatesChannel;
			set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if (!Features.IsCommunityServer) throw new GuildFeatureNotAvailableException(GuildFeatures.COMMUNITY, value);
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _PublicUpdatesChannel, value);
			}
		}
		private TextChannel? _PublicUpdatesChannel = null;
		private Snowflake? PublicUpdatesChannelID = 0;

		/// <summary>
		/// The the rules channel.<para/>
		/// <strong>Only available to Community Servers</strong>
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="GuildFeatureNotAvailableException">If this guild is not registered as a public server, and does not have nor need a public updates channel.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public TextChannel? RulesChannel {
			get => _RulesChannel;
			set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if (!Features.IsCommunityServer) throw new GuildFeatureNotAvailableException(GuildFeatures.COMMUNITY, value);
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _RulesChannel, value);
			}
		}
		private TextChannel? _RulesChannel = null;
		private Snowflake? RulesChannelID = 0;

		/// <summary>
		/// The the channel that system messages (e.g. join/leave + boosts) are sent to.<para/>
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public TextChannel? SystemChannel {
			get => _SystemChannel;
			set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _SystemChannel, value);
			}
		}
		private TextChannel? _SystemChannel = null;
		private Snowflake? SystemChannelID = 0;

		/// <summary>
		/// The flags that determine what is NOT sent in the system channel.
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If this is using an undefined enum.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public SystemChannelFlags SystemChannelFlags {
			get => _SystemChannelFlags ?? SystemChannelFlags.SuppressNone;
			set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if (Enum.IsDefined(typeof(SystemChannelFlags), value)) throw new ArgumentOutOfRangeException(nameof(value), INVALID_ENUM_NAME_ERROR);
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _SystemChannelFlags, value);
			}
		}
		private SystemChannelFlags? _SystemChannelFlags = SystemChannelFlags.SuppressNone;

		#endregion

		#endregion

		#region Security

		/// <summary>
		/// The selection of members that Discord's explicit content filter will be applied to.
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="ValueNotAllowedException">If this guild is a community server and the value is set too low.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If this is using an undefined enum.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public ExplicitContentFilterLevel ExplicitFilterLevel {
			get => _ExplicitFilterLevel;
			set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if (Enum.IsDefined(typeof(ExplicitContentFilterLevel), value)) throw new ArgumentOutOfRangeException(nameof(value), INVALID_ENUM_NAME_ERROR);
				if (value != ExplicitContentFilterLevel.AllMembers && Features.IsCommunityServer) {
					// Community servers must have this set to maximum.
					throw new ValueNotAllowedException(GuildFeatures.COMMUNITY, value);
				}
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _ExplicitFilterLevel, value);
			}
		}
		private ExplicitContentFilterLevel _ExplicitFilterLevel = ExplicitContentFilterLevel.Disabled;

		/// <summary>
		/// The verification level of this server, which determines restrictions applied to members joining the server.
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="ValueNotAllowedException">If this guild is a community server and it is set to none.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If this is using an undefined enum.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public VerificationLevel VerificationLevel {
			get => _VerificationLevel;
			set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if (Enum.IsDefined(typeof(VerificationLevel), value)) throw new ArgumentOutOfRangeException(nameof(value), INVALID_ENUM_NAME_ERROR);
				if (value == VerificationLevel.None && Features.IsCommunityServer) {
					throw new ValueNotAllowedException(GuildFeatures.COMMUNITY, value);
				}
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _VerificationLevel, value);
			}
		}
		private VerificationLevel _VerificationLevel = VerificationLevel.None;

		/// <summary>
		/// The MFA level that moderators of the server must have before they can actually do administrative things.
		/// </summary>
		public MFALevel MFALevel { get; private set; }

		#endregion

		#region Server Core Data

		/// <summary>
		/// If this server was made by a bot, this is the ID of the bot's application that made it (not the ID of the bot user itself!)
		/// </summary>
		public Snowflake? ApplicationID { get; private set; }

		/// <summary>
		/// The features this server has available.
		/// </summary>
		public GuildFeatureInformation Features { get; } = new GuildFeatureInformation();

		/// <summary>
		/// The AFK channel, or <see langword="null"/> if there is none.
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public VoiceChannel? AFKChannel {
			get => _AFKChannel;
			set {
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);
				SetProperty(ref _AFKChannel, value);
			}
		}
		private VoiceChannel? _AFKChannel = null;

		/// <summary>
		/// The timeout for the AFK channel in seconds.
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public int AFKTimeout {
			get => _AFKTimeout;
			set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _AFKTimeout, value);
			}
		}
		private int _AFKTimeout = 0;

		/// <summary>
		/// Whether or not this server is considered "Large" by Discord.
		/// </summary>
		public bool IsLarge { get; private set; }

		/// <summary>
		/// The maximum number of members this server can have.
		/// </summary>
		public int MaxMembers { get; private set; }

		/// <summary>
		/// The maximum number of presences that the bot can know about, if applicable.
		/// </summary>
		public int? MaxPresences { get; private set; }

		/// <summary>
		/// The maximum amount of people that can be watching a stream at once.
		/// </summary>
		public int MaxVideoChannelUsers { get; private set; }

		/// <summary>
		/// The ID of the server's owner.
		/// </summary>
		public Snowflake OwnerID { get; private set; }

		/// <summary>
		/// Whether or not the current bot user is the owner of the server.
		/// </summary>
		public bool IAmOwner { get; private set; }

		/// <summary>
		/// Whether or not this guild is unavailable due to an outage.
		/// </summary>
		public bool Unavailable { get; private set; }

		//default_message_notifications
		/// <summary>
		/// The notification level of this server, or what messages trigger notifications.
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public GuildNotificationLevel NotificationLevel {
			get => _NotificationLevel;
			set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _NotificationLevel, value);
			}
		}
		private GuildNotificationLevel _NotificationLevel = GuildNotificationLevel.AllMessages;

		#endregion

		#region Widget

		/// <summary>
		/// Whether or not this server has its widget enabled.
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public bool WidgetEnabled {
			get => _WidgetEnabled;
			[Obsolete("This hasn't been implemented yet.", true)] set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if (!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _WidgetEnabled, value);
			}
		}
		private bool _WidgetEnabled = false;

		/// <summary>
		/// If the widget has invites enabled, this is the channel it leads to. Otherwise, this is <see langword="null"/>.
		/// </summary>
		/// <remarks>
		/// Only this property's <see langword="set"/> method will throw the given exceptions. <see langword="get"/> will never raise an exception.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectUnavailableException">If this guild is suffering from an outage.</exception>
		/// <exception cref="ObjectDeletedException">If this guild has been deleted and cannot be edited.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage the guild.</exception>
		public Snowflake? WidgetChannelID {
			get => _WidgetChannelID;
			[Obsolete("This hasn't been implemented yet.", true)] set {
				if (Unavailable) throw new ObjectUnavailableException(this, GUILD_OUTAGE);
				if(!BotMember.HasPermission(Permissions.ManageGuild)) throw new InsufficientPermissionException(Permissions.ManageGuild);

				SetProperty(ref _WidgetChannelID, value);
			}
		}
		private Snowflake? _WidgetChannelID;

		#endregion

		#region Membership, States, Roles, and Channels

		#region Membership

		/// <summary>
		/// The approximate amount of members.
		/// </summary>
		public int ApproxMemberCount { get; private set; }

		/// <summary>
		/// The approximate amount of received presences.
		/// </summary>
		public int ApproxPresenceCount { get; private set; }

		#endregion

		#region My Membership & Presence

		/// <summary>
		/// When this bot joined the guild.
		/// </summary>
		public ISO8601 JoinedGuildAt { get; private set; }

		#endregion

		/// <summary>
		/// The channels in this server. This contains any and all channel objects, including but not limited to text channels, voice channels, and channel categories.<para/>
		/// Note that this may not be in order of position. Channels in guilds implement <see cref="IComparable{T}"/>, so it is possible to use <see cref="Array.Sort{T}(T[])"/> on this.
		/// </summary>
		public IReadOnlyList<GuildChannelBase> Channels { get; internal set; }

		/// <summary>
		/// A list of the text channels in this server.
		/// Note that this may not be in order of position. Channels in guilds implement <see cref="IComparable{T}"/>, so it is possible to use <see cref="Array.Sort{T}(T[])"/> on this.
		/// </summary>
		public IReadOnlyList<TextChannel> TextChannels { get; internal set; }

		/// <summary>
		/// A list of all threads in this server. Note that it may be better to reference threads of individual <see cref="TextChannel"/>s.
		/// Note that this may not be in order of position. Channels in guilds implement <see cref="IComparable{T}"/>, so it is possible to use <see cref="Array.Sort{T}(T[])"/> on this.
		/// </summary>
		public IReadOnlyList<Thread> Threads { get; internal set; }

		/// <summary>
		/// A list of the voice channels in this server.
		/// Note that this may not be in order of position. Channels in guilds implement <see cref="IComparable{T}"/>, so it is possible to use <see cref="Array.Sort{T}(T[])"/> on this.
		/// </summary>
		public IReadOnlyList<VoiceChannel> VoiceChannels { get; internal set; }

		/// <summary>
		/// A list of channel categories in this server. These category objects contain references to the channels contained inside.
		/// Note that this may not be in order of position. Channels in guilds implement <see cref="IComparable{T}"/>, so it is possible to use <see cref="Array.Sort{T}(T[])"/> on this.
		/// </summary>
		public IReadOnlyList<ChannelCategory> ChannelCategories { get; internal set; }

		/// <summary>
		/// The emojis in this server.
		/// </summary>
		public IReadOnlyList<Emoji> Emojis { get; internal set; }

		/// <summary>
		/// The stickers in this server.
		/// </summary>
		public IReadOnlyList<Sticker> Stickers { get; internal set; }

		/// <summary>
		/// The members in this server.
		/// </summary>
		public IReadOnlyList<Member> Members => MembersStorage ?? Member.InstantiatedMembers[ID].Values.ToList();

		/// <summary>
		/// This is used for clones.
		/// </summary>
		private IReadOnlyList<Member>? MembersStorage = null;

		/// <summary>
		/// The presences of members in this server.
		/// </summary>
		public IReadOnlyList<Presence> Presences { get; internal set; }

		/// <summary>
		/// The roles in this server. Removing any roles from this array will delete the role.
		/// </summary>
		public DiscordObjectContainer<Role> Roles {
			get {
				if (_Roles == null) {
					_Roles = new DiscordObjectContainer<Role>(this, false, true) {
						ExtraRequirementDelegate = () => {
							if (!BotMember.HasPermission(Permissions.ManageGuild)) return new InsufficientPermissionException(Permissions.ManageRoles);
							return null;
						}
					};
				}
				return _Roles!;
			}
		}
		private DiscordObjectContainer<Role>? _Roles = null;
		private readonly ThreadedDictionary<Snowflake, Payloads.PayloadObjects.Role> _PayloadRoles = new ThreadedDictionary<Snowflake, Payloads.PayloadObjects.Role>();

		/// <summary>
		/// Users who are in voice channels, and their states.
		/// </summary>
		public IReadOnlyList<VoiceState> VoiceStates { get; internal set; } = new List<VoiceState>();

		#endregion

		#endregion

		#region Extended Properties

		/// <summary>
		/// The member object representing this bot in this server.
		/// </summary>
		public Member BotMember { get; private set; }

		/// <summary>
		/// Returns the <code>@everyone</code> role, which is a role with the same ID as this server.
		/// </summary>
		public Role EveryoneRole => Roles[ID]!;


		/// <summary>
		/// The maximum filesize that the bot can send in accordance with server boost, in bytes.
		/// </summary>
		public int FileSizeLimit {
			get {
				if (NitroBoostTier == PremiumTier.Tier1) {
					return 8000000;
				} else if (NitroBoostTier == PremiumTier.Tier2) {
					return 50000000;
				} else if (NitroBoostTier == PremiumTier.Tier3) {
					return 100000000;
				}
				return 8000000;
			}
		}

		#endregion

		#region Extended Methods

		#region ...To customize the server

		/// <inheritdoc/>
		[Obsolete("This hasn't been implemented yet.", true)]
		public void SetServerIcon(FileInfo newImage) {

		}

		/// <inheritdoc/>
		[Obsolete("This hasn't been implemented yet.", true)]
		public void SetServerInviteSplash(FileInfo newImage) {

		}

		/// <inheritdoc/>
		[Obsolete("This hasn't been implemented yet.", true)]
		public void SetServerBanner(FileInfo newImage) {

		}

		#endregion

		#region ...To get or change components, like...

		#region The server itself

		/// <summary>
		/// SHOULD ONLY EVER BE USED WHEN THIS GUILD WAS SHALLOWLY INITIALIZED. VERY EXPENSIVE. AVOID AT ALL COSTS.<para/>
		/// This will redownload the entire guild.
		/// </summary>
		/// <returns></returns>
		internal async Task DownloadEntireGuildAsync(bool silence = false) {
			if (!silence) ObjectLogger.WriteCritical("Something wanted this guild, but it didn't exist yet! It has been redownloaded from scratch.");
			var plGuild = (await GetGuild.ExecuteAsync<Payloads.PayloadObjects.Guild>(new APIRequestData { Params = { ID } })).Item1;
			await Update(plGuild!, false);

			// Update channels
			InstantiatedGuilds[ID] = this; // This will prevent an infinite loop when creating channels.
			await DownloadAllChannelsAsync();
			// await RedownloadAllRolesAsync();

			// DiscordClient.Current!.Events.GuildEvents.InvokeOnGuildCreated(this);
			return;
		}

		/// <summary>
		/// Returns all instantiated guilds.
		/// </summary>
		/// <returns></returns>
		public static Guild[] GetAllGuilds() => InstantiatedGuilds.Values.ToArray();

		#endregion

		#region Channels

		/// <summary>
		/// Download all of the channels in this guild by force. Avoid if possible!
		/// </summary>
		/// <returns></returns>
		internal async Task DownloadAllChannelsAsync() {
			var plChannels = (await GetGuildChannels.ExecuteAsync<Payloads.PayloadObjects.Channel[]>(new APIRequestData { Params = { ID } })).Item1!;
			SkipChannelRegistry = true;
			List<GuildChannelBase> channels = new List<GuildChannelBase>();
			List<ChannelCategory> categories = new List<ChannelCategory>();

			foreach (var channel in plChannels) {
				GuildChannelBase toAdd = await GuildChannelBase.GetOrCreateAsync<GuildChannelBase>(channel);
				channels.Add(toAdd);
			}

			Channels = channels.AsReadOnly();
			UpdateChannelRegistries();
			SkipChannelRegistry = false;

			if (PublicUpdatesChannelID != null) _PublicUpdatesChannel = (TextChannel)Channels.Where(c => c.ID == PublicUpdatesChannelID).FirstOrDefault();
			if (RulesChannelID != null) _RulesChannel = (TextChannel)Channels.Where(c => c.ID == RulesChannelID).FirstOrDefault();
			if (SystemChannelID != null) _SystemChannel = (TextChannel)Channels.Where(c => c.ID == SystemChannelID).FirstOrDefault();
		}

		#endregion

		#region Members

		/// <summary>
		/// Acquires the member with the given ID via a guaranteed redownload. As such, this can be expensive to call. Use care, and avoid calling this unless it is absolutely necessary.<para/>
		/// This will return <see langword="null"/> if, for whatever reason, the member could not be downloaded.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		/// <exception cref="WebSocketErroredException">If the request fails.</exception>
		public async Task<Member?> ForcefullyDownloadMemberAsync(Snowflake id) {
			bool existed = Member.MemberExists(this, id);
			Member? member = await Member.GetOrCreateAsync(id, this);
			if (existed && member != null) {
				// If we existed already, we returned from cache, so we need to do a manual update here.
				var memberPayload = (await GetGuildMember.ExecuteAsync<Payloads.PayloadObjects.Member>(new APIRequestData { Params = { ID, id } })).Item1;
				await member.Update(memberPayload!, false);
			}
			return member;
		}

		/// <summary>
		/// Acquires the member with the given ID from cache, or if they do not exist yet, will acquire them via a download.
		/// This will return <see langword="null"/> if, for whatever reason, the member could not be downloaded (and they did not already exist in cache).
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<Member?> GetMemberAsync(Snowflake id) {
			return Member.GetOrCreateAsync(id, this);
		}

		/// <summary>
		/// Tries to find a member with the given name by searching which members' names contain the given name string. Returns all members that qualify.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Member[] FindMembers(string name) {
			name = name.ToLower();
			List<Member> retArray = new List<Member>();
			foreach (Member member in Members) {
				if (member.Nickname != null && member.Nickname.ToLower().Contains(name)) {
					retArray.Add(member);
					continue;
				}
				if (member.FullName.ToLower().Contains(name)) {
					retArray.Add(member);
					continue;
				}
			}
			return retArray.ToArray();
		}

		/// <summary>
		/// Prunes members from the server who have been inactive for longer than <paramref name="inactiveDays"/> days, and optionally with the given roles.
		/// </summary>
		/// <param name="inactiveDays">The amount of days a given member has to have been inactive for in order to be pruned. Minimum of 1.</param>
		/// <param name="reason">Why are you pruning members?</param>
		/// <param name="withRoles">The roles to use to determine who to prune.</param>
		/// <param name="withCount">Return the amount of pruned members. Possible, but discouraged, for large servers.</param>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot cannot kick members</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="inactiveDays"/> is less than 1</exception>
		public Task PruneMembersAsync(int inactiveDays, string? reason, Role[]? withRoles = null, bool withCount = false) {
			if (!BotMember.HasPermission(Permissions.KickMembers)) {
				throw new InsufficientPermissionException(Permissions.KickMembers);
			}
			if (inactiveDays < 1) throw new ArgumentOutOfRangeException(nameof(inactiveDays));

			APIRequestData prune = new APIRequestData {
				Params = { ID },
				Reason = reason
			};

			prune.SetJsonField("days", inactiveDays);
			if (withRoles != null) prune.SetJsonField("include_roles", withRoles);
			prune.SetJsonField("compute_prune_count", withCount);
			return BeginGuildPrune.ExecuteAsync(prune);
		}

		/// <summary>
		/// <strong>WARNING: VERY EXPENSIVE</strong>. Downloads all members from the server.
		/// </summary>
		/// <returns></returns>
		public Task<Member[]> DownloadAllMembersAsync() {
			return DiscordClient.Current!.RequestAllGuildMembersAsync(ID);
		}

		/// <summary>
		/// Returns all already-downloaded members that have the given role.
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		public Member[] FindMembersWithRole(Role role) {
			if (role.Server != this) throw new ArgumentException("The given role does not exist in this server!");
			if (role.ID == ID) return Members.ToArray(); // Role with the same ID as the server is @everyone

			List<Member> retArray = new List<Member>();
			foreach (Member member in Members) {
				if (member.Roles.Contains(role)) retArray.Add(member);
			}
			return retArray.ToArray();
		}

		#endregion

		#region Kicks & Bans

		/// <summary>
		/// Returns a list of every banned user. This makes a request to discord. Please call sparingly.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the BAN_MEMBERS permission.</exception>
		public async Task<List<Ban>> GetBannedMembersAsync() {
			if (!BotMember.HasPermission(Permissions.BanMembers)) {
				throw new InsufficientPermissionException(Permissions.BanMembers);
			}
			var banPayloads = (await GetGuildBans.ExecuteAsync<List<Payloads.PayloadObjects.Ban>>(new APIRequestData { Params = { ID } })).Item1;
			List<Ban> bans = new List<Ban>(banPayloads!.Count);
			for (int i = 0; i < bans.Count; i++) {
				bans[i] = new Ban(this, banPayloads[i]);
			}
			return bans;
		}

		/// <summary>
		/// Returns the <see cref="Ban"/> object for the user with the given <paramref name="id"/>, or <see langword="null"/> if they are not banned.
		/// </summary>
		/// <param name="id">The ID of the member to check.</param>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the BAN_MEMBERS permission.</exception>
		public async Task<Ban?> GetBanInfo(Snowflake id) {
			if (!BotMember.HasPermission(Permissions.BanMembers)) {
				throw new InsufficientPermissionException(Permissions.BanMembers);
			}
			APIRequestData getBan = new APIRequestData {
				Params = { ID, id }
			};
			var ban = (await GetGuildBan.ExecuteAsync<Payloads.PayloadObjects.Ban>(getBan)).Item1;
			if (ban == null) return null;
			return new Ban(this, ban);
		}

		/// <summary>
		/// Bans the given member with the given ID, and deletes the messages they sent in the last <paramref name="deleteMessageDays"/> days, which has a maximum of 7.
		/// </summary>
		/// <param name="member">The member to ban</param>
		/// <param name="reason">Why are you banning them?</param>
		/// <param name="deleteMessageDays">The number of days of messages to delete sent by this member, from 0 to 7.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="deleteMessageDays"/> is less than 0 or greater than 7.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the BAN_MEMBERS permission.</exception>
		public async Task BanMemberAsync(Snowflake member, string? reason, int deleteMessageDays = 0) {
			if (!BotMember.HasPermission(Permissions.BanMembers)) {
				throw new InsufficientPermissionException(Permissions.BanMembers);
			}

			APIRequestData ban = new APIRequestData {
				Params = { ID, member },
				Reason = reason
			};

			ban.SetJsonField("delete_message_days", deleteMessageDays);
			await CreateGuildBan.ExecuteAsync(ban);
		}

		/// <inheritdoc cref="BanMemberAsync(Snowflake, string?, int)"/>
		public Task BanMemberAsync(Member member, string? reason, int deleteMessageDays = 0) => BanMemberAsync(member.ID, reason, deleteMessageDays);

		/// <summary>
		/// Unbans the member with the given ID.
		/// </summary>
		/// <param name="member">The member to unban.</param>
		/// <param name="reason">Why are you unbanning them?</param>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the BAN_MEMBERS permission.</exception>
		public Task UnbanMemberAsync(Snowflake member, string? reason) {
			if (!BotMember.HasPermission(Permissions.BanMembers)) {
				throw new InsufficientPermissionException(Permissions.BanMembers);
			}
			return RemoveGuildBan.ExecuteAsync(new APIRequestData { Params = { ID, member }, Reason = reason });
		}

		/// <summary>
		/// Kicks the given member for the given reason.
		/// </summary>
		/// <param name="member"></param>
		/// <param name="reason">Why are you kicking them?</param>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the KICK_MEMBERS permission.</exception>
		public Task KickMemberAsync(Snowflake member, string? reason) {
			if (!BotMember.HasPermission(Permissions.KickMembers)) {
				throw new InsufficientPermissionException(Permissions.KickMembers);
			}
			return RemoveGuildMember.ExecuteAsync(new APIRequestData { Params = { ID, member }, Reason = reason });
		}

		/// <inheritdoc cref="KickMemberAsync(Snowflake, string?)"/>
		public Task KickMemberAsync(Member member, string? reason) => KickMemberAsync(member.ID, reason);

		#endregion

		#region Roles

		/// <summary>
		/// Forces a redownload of the roles list.
		/// </summary>
		public async Task RedownloadAllRolesAsync() {
			//JObject rolesContainer = (await GetGuildRoles.Perform(ID))!;
			var payloadRoles = (await GetGuildRoles.ExecuteAsync<Payloads.PayloadObjects.Role[]>(new APIRequestData { Params = { ID } })).Item1!;
			List<Role> newRoles = new List<Role>();
			foreach (var role in payloadRoles!) {
				Role asRealRole = Role.GetOrCreate(role, this);
				await asRealRole.Update(role, false); // Update the object since we're downloading it fresh.
				newRoles.Add(asRealRole);
			}
			Roles.SetTo(newRoles);
			Roles.Reset();
		}

		/// <summary>
		/// Create a new role with the given properties.
		/// </summary>
		/// <param name="name">The name of this role.</param>
		/// <param name="allowedPermissions">The permissions that are enabled on this role. If left undefined, it inherits from @everyone</param>
		/// <param name="color">The color of this role, or null to use the default color.</param>
		/// <param name="displaySeparately">Whether or not to display this role separately in the member list.</param>
		/// <param name="mentionable">Whether or not members can @mention this role.</param>
		/// <param name="reason">The reason this role was created.</param>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot is not authorized to manage roles.</exception>
		public async Task<Role> CreateNewRoleAsync(string? name = "new role", Permissions? allowedPermissions = null, int? color = null, bool? displaySeparately = false, bool? mentionable = false, string? reason = null) {
			if (!BotMember.HasPermission(Permissions.ManageRoles)) {
				throw new InsufficientPermissionException(Permissions.ManageRoles);
			}

			APIRequestData createRole = new APIRequestData {
				Params = { ID },
				Reason = reason
			};

			if (name != null) createRole.SetJsonField("name", name);
			if (allowedPermissions != null) createRole.SetJsonField("permissions", ((int)allowedPermissions.Value).ToString());
			if (color != null) createRole.SetJsonField("color", color.Value);
			if (displaySeparately != null) createRole.SetJsonField("hoist", displaySeparately.Value);
			if (mentionable != null) createRole.SetJsonField("mentionable", mentionable.Value);

			var netRole = (await CreateGuildRole.ExecuteAsync<Payloads.PayloadObjects.Role>(createRole)).Item1;
			Role role = Role.GetOrCreate(netRole!, this);
			Roles.AddInternally(role);
			Roles.Reset();
			return role;
		}

		/// <summary>
		/// Deletes the given <see cref="Role"/> from the server. Unlike modifying <see cref="Roles"/>, this will not rely on <see cref="DiscordObject.BeginChanges"/> being called.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="reason">The reason this role was deleted.</param>
		/// <returns></returns>
		public async Task DeleteRoleAsync(Role role, string? reason = null) {
			if (!BotMember.HasPermission(Permissions.ManageRoles)) {
				throw new InsufficientPermissionException(Permissions.ManageRoles);
			}

			APIRequestData deleteRole = new APIRequestData {
				Params = { ID, role.ID },
				Reason = reason
			};

			role.Deleted = true;
			await DeleteGuildRole.ExecuteAsync(deleteRole);
			Roles.RemoveInternally(role);
			Roles.Reset();
		}

		/// <summary>
		/// Modify the given role to have the given properties. Any <see langword="null"/> properties will remain unchanged.<para/>
		/// Unlike editing the properties of a <see cref="Role"/> directly from <see cref="Roles"/>, this does not require a call to <see cref="DiscordObject.BeginChanges(bool)"/>
		/// </summary>
		/// <param name="role">The role to change.</param>
		/// <param name="name">The new name of this role.</param>
		/// <param name="allowedPermissions">The new permissions that are enabled on this role.</param>
		/// <param name="color">The new color of this role, or null to use the default color.</param>
		/// <param name="displaySeparately">Whether or not to display this role separately in the member list.</param>
		/// <param name="mentionable">Whether or not members can @mention this role.</param>
		/// <param name="reason">The reason this role is being edited.</param>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot is not authorized to manage roles.</exception>
		public async Task ModifyRoleAsync(Role role, string? name = null, Permissions? allowedPermissions = null, int? color = null, bool? displaySeparately = null, bool? mentionable = null, string? reason = null) {
			if (!BotMember.HasPermission(Permissions.ManageRoles)) {
				throw new InsufficientPermissionException(Permissions.ManageRoles);
			}

			APIRequestData modRole = new APIRequestData {
				Params = { ID, role.ID },
				Reason = reason
			};

			if (name != null) modRole.SetJsonField("name", name);
			if (allowedPermissions != null) modRole.SetJsonField("permissions", ((int)allowedPermissions.Value).ToString());
			if (color != null) modRole.SetJsonField("color", color.Value);
			if (displaySeparately != null) modRole.SetJsonField("hoist", displaySeparately.Value);
			if (mentionable != null) modRole.SetJsonField("mentionable", mentionable.Value);

			var netRole = (await ModifyGuildRole.ExecuteAsync<Payloads.PayloadObjects.Role>(modRole)).Item1;
			await role.Update(netRole!, false);
		}

		/// <summary>
		/// Returns the <see cref="Role"/> with the given ID, or <see langword="null"/> if the role does not exist in this server.<para/>
		/// Of course, this will not download the role.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Role? GetRole(Snowflake id) {
			return ((IEnumerable<Role>)Roles).FirstOrDefault(role => role.ID == id);
		}

		#endregion

		#region Channels

		/// <summary>
		/// Forcefully redownloads all channels and populates all arrays. Ignores the object's locked state.
		/// </summary>
		/// <returns></returns>
		public async Task ForcefullyAcquireChannelsAsync() {
			var plChannels = (await GetGuildChannels.ExecuteAsync<List<Payloads.PayloadObjects.Channel>>(new APIRequestData { Params = { ID } })).Item1;
			// Only sent in the guild create event, so this may be null.
			List<GuildChannelBase> channels = new List<GuildChannelBase>();
			List<ChannelCategory> categories = new List<ChannelCategory>();

			foreach (var channel in plChannels!) {
				GuildChannelBase toAdd = await GuildChannelBase.GetOrCreateAsync<GuildChannelBase>(channel);
				channels.Add(toAdd);
			}

			Channels = channels.AsReadOnly();
			UpdateChannelRegistries();
		}

		/// <summary>
		/// Returns the channel with the given ID. This will not download the channel.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public GuildChannelBase? GetChannel(Snowflake id) {
			return Channels?.FirstOrDefault(channel => channel.ID == id);
		}

		/// <summary>
		/// Returns the channel with the given ID, as the given channel type. This will not download the channel. Returns <see langword="null"/> if it could not be returned.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public T? GetChannel<T>(Snowflake id) where T : GuildChannelBase {
			Type t = typeof(T);
			return (T?)Channels?.FirstOrDefault(channel => channel.ID == id && channel.GetType().IsAssignableFrom(t));
		}


		#endregion

		#region Emojis

		/// <summary>
		/// Downloads all emojis for this server, and updates <see cref="Emojis"/> if they were successfully downloaded.
		/// </summary>
		/// <returns></returns>
		[Obsolete("Not ready for use", true)]
		public async Task<List<Emoji>?> GetAllEmojisAsync() {
			(List<Emoji>? res, HttpResponseMessage? msg) = await GetServerEmojis.ExecuteAsync<List<Emoji>>(new APIRequestData {
				Params = {
					ID
				}
			});
			if (res != null && msg != null && msg.IsSuccessStatusCode) {
				Emojis = res;
			}
			return res;
		}

		#endregion

		#region Logs

		/// <summary>
		/// Downloads the entire audit log for this guild. Requires <see cref="Permissions.ViewAuditLog"/>
		/// </summary>
		/// <param name="byType">If defined, this will limit the type of entry downloaded.</param>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot cannot view audit logs.</exception>
		public async Task<AuditLogObject> DownloadAuditLog(AuditLogActionType? byType = null) {
			APIRequestData req = new APIRequestData { Params = { ID } };
			if (byType != null) {
				req.SetJsonField("action_type", (int)byType.Value);
			}
			(var pl, _) = await GetAuditLog.ExecuteAsync<Payloads.PayloadObjects.AuditLog>(req);
			return AuditLogObject.FromPayload(this, pl!);
		}

		#endregion

		#endregion

		#endregion

		#region Internals

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
		private Guild(Snowflake id) : base(id.Value) {
			InstantiatedGuilds[id] = this;
		}
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

		/// <summary>
		/// Gets an existing guild by this ID or creates a new one. Returns <see langword="true"/> if a new guild was created. Sets the <see cref="Unavailable"/> property to true.<para/>
		/// This expects a payload from the GUILD_DELETE or GUILD_CREATE event.
		/// </summary>
		/// <param name="plGuild"></param>
		/// <returns></returns>
		internal static Guild GetOrCreateUnavailableFromPayload(Payloads.PayloadObjects.UnavailableGuild plGuild) {
			Guild guild = InstantiatedGuilds.GetOrDefault(plGuild.ID, new Guild(plGuild.ID));
			guild.Unavailable = true;
			return guild;
		}

		/// <summary>
		/// Returns a guild object that already exists from the given paylaod (and updates it), or creates a new one from thsi paylaod.
		/// </summary>
		/// <param name="plGuild"></param>
		/// <returns></returns>
		internal static async Task<Guild> GetOrCreateFromPayload(Payloads.PayloadObjects.Guild plGuild) {
			Guild guild = InstantiatedGuilds.GetOrDefault(plGuild.ID, new Guild(plGuild.ID));
			guild.Unavailable = false;
			await guild.Update(plGuild, false);
			return guild;
		}

		/// <summary>
		/// Returns a guild object that already exists with the given ID, or creates a new one with the given ID and downloads it.
		/// </summary>
		/// <param name="guildID"></param>
		/// <param name="silence">Silence the download alert that's used to deter calling the method.</param>
		/// <returns></returns>
		public static async Task<Guild> GetOrDownloadAsync(Snowflake guildID, bool silence = false) {
			if (InstantiatedGuilds.ContainsKey(guildID)) {
				Guild ret = InstantiatedGuilds[guildID];
				return ret;
			}
			Guild server = new Guild(guildID);
			await server.DownloadEntireGuildAsync(silence);
			return server;
		}

		#endregion

		#region Implementation

		private bool SkipChannelRegistry = false;

		/// <summary>
		/// Adds the given channel to this guild's channel registry unless the guild is currently in the middle of an update cycle or it already contains this channel.
		/// </summary>
		/// <param name="channelIn"></param>
		internal void RegisterChannel(GuildChannelBase channelIn) {
			if (SkipChannelRegistry) return;
			if (Channels.Contains(channelIn)) return;
			List<GuildChannelBase> channels = Channels.ToList();
			channels.Add(channelIn);
			Channels = channels;
			UpdateChannelRegistries();
		}

		/// <summary>
		/// Updates <see cref="TextChannels"/>, <see cref="VoiceChannels"/>, <see cref="ChannelCategories"/>, and <see cref="Threads"/> once <see cref="Channels"/> has been modified.
		/// </summary>
		protected private void UpdateChannelRegistries() {
			TextChannels = Channels.Where(channel => channel.Type == ChannelType.Text).ToType<GuildChannelBase, TextChannel>().ToList().AsReadOnly();
			VoiceChannels = Channels.Where(channel => channel.Type == ChannelType.Voice).ToType<GuildChannelBase, VoiceChannel>().ToList().AsReadOnly();
			ChannelCategories = Channels.Where(channel => channel.Type == ChannelType.Category).ToType<GuildChannelBase, ChannelCategory>().ToList().AsReadOnly();
			Threads = Channels.Where(channel => channel.Type.IsThreadChannel()).ToType<GuildChannelBase, Thread>().ToList().AsReadOnly();
			foreach (GuildChannelBase channel in Channels) {
				if (!(channel is ChannelCategory)) {
					if (channel.ParentID != null) {
						if (channel is Thread thread) {
							thread._ParentChannel = TextChannels.FirstOrDefault(chn => chn.ID == thread.ParentID);
						} else {
							channel._ParentCategory = ChannelCategories.FirstOrDefault(cat => cat.ID == channel.ParentID);
						}
					}
				}
			}
		}

		/// <inheritdoc/>
		protected internal override async Task Update(PayloadDataObject obj, bool skipNonNullFields) {
			if (obj is Payloads.PayloadObjects.Guild guild) {
				Unavailable = AppropriateNullableValue(Unavailable, guild.Unavailable, skipNonNullFields);

				// AFK channel is down below channel defs
				_AFKTimeout = AppropriateValue(AFKTimeout, guild.AFKTimeout, skipNonNullFields);
				ApplicationID = AppropriateValue<Snowflake?>(ApplicationID, guild.ApplicationID, skipNonNullFields);
				ApproxMemberCount = AppropriateNullableValue(ApproxMemberCount, guild.ApproxMemberCount, skipNonNullFields);
				ApproxPresenceCount = AppropriateNullableValue(ApproxPresenceCount, guild.ApproxPresenceCount, skipNonNullFields);
				BannerHash = AppropriateNullableString(BannerHash, guild.BannerHash, skipNonNullFields);

				// CHANNELS //
				if (guild.Channels != null) {
					SkipChannelRegistry = true;
					// Only sent in the guild create event, so this may be null.
					List<GuildChannelBase> channels = new List<GuildChannelBase>();
					List<ChannelCategory> categories = new List<ChannelCategory>();

					foreach (var channel in guild.Channels) {
						// FOR COMPAT:
						channel.GuildID = ID;

						GuildChannelBase toAdd = await GuildChannelBase.GetOrCreateAsync<GuildChannelBase>(channel);
						channels.Add(toAdd);
					}

					Channels = channels.AsReadOnly();
					UpdateChannelRegistries();
					SkipChannelRegistry = false;
				} else {
					if (Channels == null) Channels = new List<GuildChannelBase>();
				}

				//////////////
				Description = AppropriateNullableString(Description, guild.Description, skipNonNullFields);
				DiscoverySplashHash = AppropriateNullableString(DiscoverySplashHash, guild.DiscoverySplash, skipNonNullFields);

				// EMOJIS & STICKERS //
				List<Emoji> emojis = new List<Emoji>();
				List<Sticker> stickers = new List<Sticker>();
				foreach (var emoji in guild.Emojis) {
					if (emoji.ID != null) {
						emojis.Add(CustomEmoji.GetOrCreate(emoji));
					}  else {
						emojis.Add(Emoji.GetOrCreate(emoji.Name!));
					}
				}
				foreach (var sticker in guild.Stickers) {
					stickers.Add(Sticker.GetOrCreate(sticker));
				}
				Emojis = emojis.AsReadOnly();
				Stickers = stickers.AsReadOnly();
				////////////

				_ExplicitFilterLevel = guild.ExplicitFilterLevel;
				Features.SetToFeatures(guild.Features);
				IAmOwner = AppropriateNullableValue(IAmOwner, guild.Owner ?? guild.OwnerID == User.BotUser?.ID, skipNonNullFields);
				IsLarge = AppropriateNullableValue(IsLarge, guild.IsLarge, skipNonNullFields);
				JoinedGuildAt = AppropriateNullableValue(JoinedGuildAt, guild.JoinedGuildAt, skipNonNullFields);
				MaxMembers = AppropriateNullableValue(MaxMembers, guild.MaxMembers, skipNonNullFields);
				MaxPresences = AppropriateValue(MaxPresences, guild.MaxPresences, skipNonNullFields);
				MaxVideoChannelUsers = AppropriateNullableValue(MaxVideoChannelUsers, guild.MaxVideoChannelUsers, skipNonNullFields);

				MFALevel = AppropriateValue(MFALevel, guild.MFALevel, skipNonNullFields);
				_Name = AppropriateValue(Name, guild.Name, skipNonNullFields);
				NitroBoosterCount = AppropriateNullableValue(NitroBoosterCount, guild.PremiumSubscriberCount, skipNonNullFields);
				NitroBoostTier = AppropriateValue(NitroBoostTier, guild.PremiumTier, skipNonNullFields);
				OwnerID = AppropriateNullableValue(OwnerID, guild.OwnerID, skipNonNullFields);
				_PreferredLocale = AppropriateValue(PreferredLocale, guild.PreferredLocale, skipNonNullFields);

				// PRESENCES //
				if (guild.Presences != null) {
					List<Presence> presences = new List<Presence>();
					foreach (var presence in guild.Presences) {
						presences.Add(new Presence(presence));
					}
					Presences = presences.AsReadOnly();
				}

				PublicUpdatesChannelID = AppropriateValue((ulong?)PublicUpdatesChannelID, guild.PublicUpdatesChannelID, skipNonNullFields);
				RulesChannelID = AppropriateValue((ulong?)RulesChannelID, guild.RulesChannelID, skipNonNullFields);
				SystemChannelID = AppropriateValue((ulong?)SystemChannelID, guild.SystemChannelID, skipNonNullFields);


				// SPECIAL CHANNELS //
				if ((skipNonNullFields && _PublicUpdatesChannel == null) || !skipNonNullFields) {
					if (guild.PublicUpdatesChannelID != null) {
						_PublicUpdatesChannel = (TextChannel)Channels.Where(channel => channel.ID == guild.PublicUpdatesChannelID.Value).FirstOrDefault();
					} else {
						_PublicUpdatesChannel = null;
					}
				}
				if ((skipNonNullFields && _RulesChannel == null) || !skipNonNullFields) {
					if (guild.RulesChannelID != null) {
						_RulesChannel = (TextChannel)Channels.Where(channel => channel.ID == guild.RulesChannelID.Value).FirstOrDefault();
					} else {
						_RulesChannel = null;
					}
				}
				if ((skipNonNullFields && _SystemChannel == null) || !skipNonNullFields) {
					if (guild.SystemChannelID != null) {
						_SystemChannel = (TextChannel)Channels.Where(channel => channel.ID == guild.SystemChannelID.Value).FirstOrDefault();
					} else {
						_SystemChannel = null;
					}
				}
				if ((skipNonNullFields && _AFKChannel == null) || !skipNonNullFields) {
					if (guild.AFKChannelID != null) {
						_AFKChannel = VoiceChannels.Where(channel => channel.ID == guild.AFKChannelID.Value).FirstOrDefault();
					} else {
						_AFKChannel = null;
					}
				}

				// ROLES //
				if (guild.Roles != null) {
					List<Role> roles = new List<Role>();
					_PayloadRoles.Clear();
					foreach (var role in guild.Roles) {
						//Role.GetOrCreate(role, this, out Role rObj);
						Role rObj = Role.GetOrCreate(role, this);
						await rObj.Update(role, false);
						roles.Add(rObj);
						_PayloadRoles[role.ID] = role;
					}
					Roles!.SetTo(roles);
				}

				ServerIconHash = AppropriateNullableString(ServerIconHash, guild.IconHash, skipNonNullFields);
				_SystemChannelFlags = AppropriateValue(_SystemChannelFlags, guild.SystemChannelFlags, skipNonNullFields);
				if (guild.Unavailable != null) Unavailable = guild.Unavailable.Value;
				VanityURLCode = AppropriateNullableString(VanityURLCode, guild.VanityURLCode, skipNonNullFields);
				_VerificationLevel = guild.VerificationLevel;
				_VoiceRegion = AppropriateNullableString(_VoiceRegion, guild.VoiceRegion, skipNonNullFields)!;

				_WidgetChannelID = AppropriateValue(_WidgetChannelID, (Snowflake?)guild.WidgetChannelID, skipNonNullFields);
				_WidgetEnabled = AppropriateValue(_WidgetEnabled, guild.WidgetEnabled, skipNonNullFields);

				// MEMBERS //
				if (guild.Members != null) {
					List<Member> members = new List<Member>();
					foreach (var member in guild.Members) {
						if (member.User?.UserID == 0) {
							ObjectLogger.WriteCritical("A member existed with an ID of 0!");
							continue;
						}
						if (Member.MemberExists(this, member.User!.UserID)) {
							// This may occur if the server went down and is reloading.
							//Member.GetOrCreate(member.User!.UserID, this, out Member mem);
							Member? mem = await Member.GetOrCreateAsync(member.User!.UserID, this);
							if (mem != null) {
								// Theoretically this shouldn't happen, just want to play it safe.
								await mem.Update(member, true);
								members.Add(mem);
							}
						} else {
							members.Add(new Member(this, member));
						}
					}

					// VOICE STATES //
					List<VoiceState> newStates = new List<VoiceState>();
					if (guild.VoiceStates != null) {
						foreach (var state in guild.VoiceStates) {
							VoiceState existingState = VoiceState.GetStateForOrCreate(state.UserID);
							existingState.UpdateFrom(this, state);
							newStates.Add(existingState);
						}
						VoiceStates = newStates;
					}

					foreach (VoiceChannel ch in VoiceChannels) {
						ch.UpdateVoiceStatesInternal();
					}
				}

				BotMember = (await Member.GetOrCreateAsync(User.BotUser!.ID, this))!;
			} else if (obj is Payloads.PayloadObjects.UnavailableGuild) {
				Unavailable = true;
			}
		}

		/// <summary>
		/// Intended to be called from <see cref="Payloads.Events.Intents.GuildVoiceStates.VoiceStateUpdateEvent"/>, this will add a new <see cref="VoiceState"/> to <see cref="VoiceStates"/> (if necessary), and tell all voice channels to update the member count.
		/// </summary>
		/// <param name="newState"></param>
		internal void RegisterAndUpdateVoiceState(VoiceState newState) {
			// Register this state if needed.
			if (!VoiceStates.Contains(newState)) {
				List<VoiceState> newStates = VoiceStates.ToList();
				newStates.Add(newState);
				VoiceStates = newStates.AsReadOnly();
			}
			foreach (VoiceChannel channel in VoiceChannels) {
				channel.UpdateVoiceStatesInternal();
			}
		}

		/// <inheritdoc/>
		protected override async Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changes, string? reasons) {
			APIRequestData modifyGuild = new APIRequestData {
				Params = { ID },
				Reason = reasons
			};

			if (changes.ContainsKey(nameof(Name))) modifyGuild.SetJsonField("name", Name);
			if (changes.ContainsKey(nameof(VoiceRegion))) modifyGuild.SetJsonField("region", VoiceRegion);
			if (changes.ContainsKey(nameof(VerificationLevel))) modifyGuild.SetJsonField("verification_level", (int)VerificationLevel);
			if (changes.ContainsKey(nameof(NotificationLevel))) modifyGuild.SetJsonField("default_message_notifications", (int)NotificationLevel);
			if (changes.ContainsKey(nameof(ExplicitFilterLevel))) modifyGuild.SetJsonField("explicit_content_filter", (int)ExplicitFilterLevel);
			if (changes.ContainsKey(nameof(AFKChannel))) modifyGuild.SetJsonField("afk_channel_id", AFKChannel?.ID);
			if (changes.ContainsKey(nameof(AFKTimeout))) modifyGuild.SetJsonField("afk_timeout", AFKTimeout);
			// icon
			// owner id
			// splash
			// banner
			if (changes.ContainsKey(nameof(SystemChannel))) modifyGuild.SetJsonField("system_channel_id", SystemChannel?.ID);
			if (changes.ContainsKey(nameof(RulesChannel))) modifyGuild.SetJsonField("rules_channel_id", RulesChannel?.ID);
			if (changes.ContainsKey(nameof(PublicUpdatesChannel))) modifyGuild.SetJsonField("public_updates_channel_id", PublicUpdatesChannel?.ID);
			if (changes.ContainsKey(nameof(PreferredLocale))) modifyGuild.SetJsonField("preferred_locale", PreferredLocale);
			HttpResponseMessage? response = null;
			if (modifyGuild.GetJson() != "{}") {
				response = await ModifyGuild.ExecuteAsync(modifyGuild);
			}

			if (changes.ContainsKey(nameof(Roles))) {
				foreach (Role? newRole in Roles) {
					if (newRole!.Deleted) {
						// ^ Removing from Roles sets deleted=true
						var resp = await DeleteGuildRole.ExecuteAsync(new APIRequestData {
							Params = { ID, newRole!.ID },
							Reason = reasons
						});
						if (response == null) response = resp;
					} else {
						//var inPayload = _PayloadRoles.Where(plRole => newRole!.ID == plRole.ID).FirstOrDefault();
						if (_PayloadRoles.TryGetValue(newRole!.ID, out var inPayload)) {
							//int idx = _PayloadRoles.IndexOf(inPayload);

							APIRequestData modRole = new APIRequestData {
								Params = { ID, newRole!.ID },
								Reason = reasons
							};

							if (inPayload.Name != newRole!.Name) modRole.SetJsonField("name", newRole!.Name);
							if (inPayload.Permissions != newRole!.Permissions.GetAllowed()) modRole.SetJsonField("permissions", newRole!.Permissions);
							if (inPayload.Color != newRole!.Color.GetValueOrDefault().Value) modRole.SetJsonField("color", newRole!.Color.GetValueOrDefault().Value);
							if (inPayload.Hoisted != newRole!.Hoisted) modRole.SetJsonField("hoist", newRole!.Hoisted);
							if (inPayload.Mentionable != newRole!.Mentionable) modRole.SetJsonField("mentionable", newRole!.Mentionable);

							(var r, var resp) = await ModifyGuildRole.ExecuteAsync<Payloads.PayloadObjects.Role>(modRole);
							//_PayloadRoles[idx] = r!; // Enqueue this update and wait on it.
							_PayloadRoles[inPayload.ID] = r!;
							if (response == null) response = resp;
						}
					}
				}
				Roles.Reset();
			}
			return response;
		}

		#endregion

		/// <inheritdoc/>
		public override DiscordObject MemberwiseClone() {
			Guild copy = (Guild)base.MemberwiseClone();
			copy.Channels = Channels.LazyCopy();
			copy.TextChannels = TextChannels.LazyCopy();
			copy.VoiceChannels = VoiceChannels.LazyCopy();
			copy.Threads = Threads.LazyCopy();
			copy.ChannelCategories = ChannelCategories.LazyCopy();
			copy.Emojis = Emojis.LazyCopy();
			copy.MembersStorage = Members.LazyCopy();
			copy.Presences = Presences.LazyCopy();
			copy._Roles = Roles.Clone();
			copy.VoiceStates = VoiceStates.LazyCopy();
			return copy;
		}
	}
}

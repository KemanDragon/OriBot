using EtiBotCore.Data;
using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.Events.Intents.GuildPresences;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a server.
	/// </summary>
	internal class Guild : PayloadDataObject {

		/// <summary>
		/// The ID of this server.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// The name of this server.
		/// </summary>
		[JsonProperty("name"), JsonRequired]
		public string Name { get; set; } = Constants.UNSENT_STRING_DEFAULT;

		/// <summary>
		/// The hash of the server's icon.
		/// </summary>
		[JsonProperty("icon")]
		public string? IconHash { get; set; } = Constants.UNSENT_STRING_DEFAULT;

		/// <summary>
		/// The hash of the server's splash image (on the invite page).
		/// </summary>
		[JsonProperty("splash")]
		public string? SplashHash { get; set; } = Constants.UNSENT_STRING_DEFAULT;

		/// <summary>
		/// The hash of the server's discovery page image.
		/// </summary>
		[JsonProperty("discovery_splash")]
		public string? DiscoverySplash { get; set; } = Constants.UNSENT_STRING_DEFAULT;

		/// <summary>
		/// Whether or not the current client receiving this <see cref="Guild"/> is the owner of the server.<para/>
		/// <strong>This is only sent when this <see cref="Guild"/> was acquired via the GET Current User Guilds endpoint.</strong>
		/// </summary>
		[JsonProperty("owner", NullValueHandling = NullValueHandling.Ignore)]
		public bool? Owner { get; set; }

		/// <summary>
		/// The user ID of the owner of the server.
		/// </summary>
		[JsonProperty("owner_id")]
		public ulong OwnerID { get; set; }

		[JsonProperty("permissions", NullValueHandling = NullValueHandling.Ignore)]
		private string? MyPermissionsString { get; set; } = Constants.UNSENT_STRING_DEFAULT;

		/// <summary>
		/// The permissions that the current client receiving this <see cref="Guild"/> has (excluding overwrites)<para/>
		/// <strong>This is only sent when this <see cref="Guild"/> was acquired via the GET Current User Guilds endpoint.</strong>
		/// </summary>
		[JsonIgnore] public Permissions? MyPermissions {
			get {
				if (MyPermissionsString == null) return null;
				return (Permissions)ulong.Parse(MyPermissionsString);
			}
			set {
				if (value == null) {
					MyPermissionsString = null;
				} else {
					MyPermissionsString = ((ulong)value).ToString();
				}
			}
		}

		/// <summary>
		/// The voice region ID for the guild. This corresponds to the ID field of a voice region object.
		/// </summary>
		[JsonProperty("region")]
		public string VoiceRegion { get; set; } = Constants.UNSENT_STRING_DEFAULT;

		/// <summary>
		/// The ID of the afk channel, or <see langword="null"/> if it is not set.
		/// </summary>
		[JsonProperty("afk_channel_id")]
		public ulong? AFKChannelID { get; set; }

		/// <summary>
		/// The timeout for AFK in seconds.
		/// </summary>
		[JsonProperty("afk_timeout")]
		public int AFKTimeout { get; set; }

		/// <summary>
		/// Whether or not the server has its widget enabled.
		/// </summary>
		[JsonProperty("widget_enabled")]
		public bool WidgetEnabled { get; set; }

		/// <summary>
		/// The channel ID that the widget will generate an invite to, or <see langword="null"/> if it's set to no invite.
		/// </summary>
		[JsonProperty("widget_channel_id")]
		public ulong? WidgetChannelID { get; set; }

		/// <summary>
		/// The verification level required to enter the guild.
		/// </summary>
		[JsonProperty("verification_level"), JsonConverter(typeof(EnumConverter))]
		public VerificationLevel VerificationLevel { get; set; }

		/// <summary>
		/// The default message notifications level.
		/// </summary>
		[JsonProperty("default_message_notifications"), JsonConverter(typeof(EnumConverter))]
		public GuildNotificationLevel NotificationLevel { get; set; }

		/// <summary>
		/// The explicit content filter level.
		/// </summary>
		[JsonProperty("explicit_content_filter"), JsonConverter(typeof(EnumConverter))]
		public ExplicitContentFilterLevel ExplicitFilterLevel { get; set; }

		/// <summary>
		/// The roles in this guild.
		/// </summary>
		[JsonProperty("roles"), JsonRequired]
		public Role[] Roles { get; set; } = new Role[0];

		/// <summary>
		/// The emojis in this guild.
		/// </summary>
		[JsonProperty("emojis"), JsonRequired]
		public Emoji[] Emojis { get; set; } = new Emoji[0];

		/// <summary>
		/// The features this guild can use. See <see cref="GuildFeatures"/> for possible entries.
		/// </summary>
		[JsonProperty("features"), JsonRequired]
		public string[] Features { get; set; } = new string[0];

		/// <summary>
		/// The 2FA level required for this guild.
		/// </summary>
		[JsonProperty("mfa_level"), JsonConverter(typeof(EnumConverter))]
		public MFALevel MFALevel { get; set; }

		/// <summary>
		/// If a bot created this guild, this is the application's ID.
		/// </summary>
		[JsonProperty("application_id")]
		public ulong? ApplicationID { get; set; }

		/// <summary>
		/// If the guild has system messages enabled (e.g. join/leave, boost) this is the channel ID that they are sent to.
		/// </summary>
		[JsonProperty("system_channel_id")]
		public ulong? SystemChannelID { get; set; }

		/// <summary>
		/// What types of messages are not sent in <see cref="SystemChannelID"/>.
		/// </summary>
		[JsonProperty("system_channel_flags"), JsonConverter(typeof(EnumConverter))]
		public SystemChannelFlags SystemChannelFlags { get; set; }

		/// <summary>
		/// The ID of the rules channel for guilds with the PUBLIC feature enabled.
		/// </summary>
		[JsonProperty("rules_channel_id")]
		public ulong? RulesChannelID { get; set; }

		/// <summary>
		/// The ISO8601 timestamp for when this server was joined.<para/>
		/// <strong>This is only sent in the GUILD_CREATE event.</strong>
		/// </summary>
		[JsonProperty("joined_at", NullValueHandling = NullValueHandling.Ignore), JsonConverter(typeof(TimestampConverter))]
		public ISO8601? JoinedGuildAt { get; set; }

		/// <summary>
		/// Whether or not this boolean is considered a large guild.<para/>
		/// <strong>This is only sent in the GUILD_CREATE event.</strong>
		/// </summary>
		[JsonProperty("large", NullValueHandling = NullValueHandling.Ignore)]
		public bool? IsLarge { get; set; } // Bool here isn't sent because it's useless outside of the create context.

		/// <summary>
		/// Whether or not this guild is unavailable due to an outage.<para/>
		/// <strong>This is only sent in the GUILD_CREATE event.</strong>
		/// </summary>
		[JsonProperty("unavailable", NullValueHandling = NullValueHandling.Ignore)]
		public bool? Unavailable { get; set; } // Same here

		/// <summary>
		/// The amount of members in this server.<para/>
		/// <strong>This is only sent in the GUILD_CREATE event.</strong>
		/// </summary>
		[JsonProperty("member_count", NullValueHandling = NullValueHandling.Ignore)]
		public int? MemberCount { get; set; } // And here

		/// <summary>
		/// An array of partial voice state objects. They all lack the guild_id key.<para/>
		/// <strong>This is only sent in the GUILD_CREATE event.</strong>
		/// </summary>
		[JsonProperty("voice_states", NullValueHandling = NullValueHandling.Ignore)]
		public VoiceState[]? VoiceStates { get; set; }

		/// <summary>
		/// The users who are in this server.<para/>
		/// <strong>This is only sent in the GUILD_CREATE event.</strong>
		/// </summary>
		[JsonProperty("members", NullValueHandling = NullValueHandling.Ignore)]
		public Member[]? Members { get; set; }

		/// <summary>
		/// The channels in this server.<para/>
		/// <strong>This is only sent in the GUILD_CREATE event.</strong>
		/// </summary>
		[JsonProperty("channels", NullValueHandling = NullValueHandling.Ignore)]
		public Channel[]? Channels { get; set; }

		/// <summary>
		/// An array of partial presence updates. If this guild has more members than the large threshold, this will only include members that aren't offline and skip all offline members.<para/>
		/// <strong>This is only sent in the GUILD_CREATE event.</strong>
		/// </summary>
		[JsonProperty("presences", NullValueHandling = NullValueHandling.Ignore)]
		public PresenceUpdateEvent[]? Presences { get; set; }

		/// <summary>
		/// The maximum number of presences for the guild.<para/>
		/// <strong>Default:</strong> <c>25000</c>
		/// </summary>
		[JsonProperty("max_presences", NullValueHandling = NullValueHandling.Ignore)]
		public int? MaxPresences { get; set; } = 25000;

		/// <summary>
		/// The amount of members this guild can hold.
		/// </summary>
		[JsonProperty("max_members", NullValueHandling = NullValueHandling.Ignore)]
		public int? MaxMembers { get; set; }

		/// <summary>
		/// The vanity URL code for the guild.
		/// </summary>
		[JsonProperty("vanity_url_code")]
		public string? VanityURLCode { get; set; } = Constants.UNSENT_STRING_DEFAULT;

		/// <summary>
		/// The description of the guild, if it's discoverable.
		/// </summary>
		[JsonProperty("description")]
		public string? Description { get; set; } = Constants.UNSENT_STRING_DEFAULT;

		/// <summary>
		/// The hash of the banner image for this guild.
		/// </summary>
		[JsonProperty("banner")]
		public string? BannerHash { get; set; } = Constants.UNSENT_STRING_DEFAULT;

		/// <summary>
		/// The server's boost level.
		/// </summary>
		[JsonProperty("premium_tier"), JsonConverter(typeof(EnumConverter))]
		public PremiumTier PremiumTier { get; set; }

		/// <summary>
		/// The amount of server boosters.
		/// </summary>
		[JsonProperty("premium_subscription_count", NullValueHandling = NullValueHandling.Ignore)]
		public int? PremiumSubscriberCount { get; set; }

		/// <summary>
		/// The preferred locale for the server if it's public.
		/// </summary>
		[JsonProperty("preferred_locale"), JsonRequired]
		public string PreferredLocale { get; set; } = "en-US";

		/// <summary>
		/// The ID where adminstrators of guilds recieve updates from Discord in public servers.
		/// </summary>
		[JsonProperty("public_updates_channel_id")]
		public ulong? PublicUpdatesChannelID { get; set; }

		/// <summary>
		/// The maximum amount of users in a video channel.
		/// </summary>
		[JsonProperty("max_video_channel_users", NullValueHandling = NullValueHandling.Ignore)]
		public int? MaxVideoChannelUsers { get; set; }

		/// <summary>
		/// The approximate amount of members in the guild, which is returned when this <see cref="Guild"/> is acquired with <c>GET /guild/&lt;id&gt;</c> when <c>with_counts</c> is <see langword="true"/>
		/// </summary>
		[JsonProperty("approximate_member_count", NullValueHandling = NullValueHandling.Ignore)]
		public int? ApproxMemberCount { get; set; }

		/// <summary>
		/// The approximate number of non-offline members in thsi guild, which is returned when this <see cref="Guild"/> is acquired with <c>GET /guild/&lt;id&gt;</c> when <c>with_counts</c> is <see langword="true"/>
		/// </summary>
		[JsonProperty("approximate_presence_count", NullValueHandling = NullValueHandling.Ignore)]
		public int? ApproxPresenceCount { get; set; }

		// welcome screen object?

		/// <summary>
		/// The NSFW level of this server, which describes age restrictions and whatnot.
		/// </summary>
		[JsonProperty("nsfw_level")]
		public int NSFWLevel { get; set; }

		// stages?

		/// <summary>
		/// The stickers in this server.
		/// </summary>
		[JsonProperty("stickers")]
		public Sticker[] Stickers { get; set; } = new Sticker[0];

	}
}

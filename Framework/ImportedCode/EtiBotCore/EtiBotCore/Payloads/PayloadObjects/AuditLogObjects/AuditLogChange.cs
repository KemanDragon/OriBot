using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects.AuditLogObjects {

	/// <summary>
	/// A change made to an object in the audit log.
	/// </summary>
	internal class AuditLogChange {

		/// <summary>
		/// Guild, name
		/// </summary>
		[JsonProperty("name")]
		public string? Name { get; set; }

		/// <summary>
		/// Guild, icon
		/// </summary>
		[JsonProperty("icon_hash")]
		public string? IconHash { get; set; }

		/// <summary>
		/// Guild, splash image
		/// </summary>
		[JsonProperty("splash_hash")]
		public string? SplashHash { get; set; }

		/// <summary>
		/// Guild, owner
		/// </summary>
		[JsonProperty("owner_id")]
		public ulong? OwnerID { get; set; }

		/// <summary>
		/// Guild, voice region
		/// </summary>
		[JsonProperty("region")]
		public string? Region { get; set; }

		/// <summary>
		/// Guild, afk channel
		/// </summary>
		[JsonProperty("afk_channel_id")]
		public ulong? AFKChannelID { get; set; }

		/// <summary>
		/// Guild, afk timeout
		/// </summary>
		[JsonProperty("afk_timeout")]
		public int? AFKTimeout { get; set; }

		/// <summary>
		/// Guild, 2fa level
		/// </summary>
		[JsonProperty("mfa_level")]
		public MFALevel? MFALevel { get; set; }

		/// <summary>
		/// Guild, verification
		/// </summary>
		[JsonProperty("verification_level")]
		public VerificationLevel? VerificationLevel { get; set; }

		/// <summary>
		/// Guild, nsfw filter
		/// </summary>
		[JsonProperty("explicit_content_filter")]
		public ExplicitContentFilterLevel? ExplicitFilterLevel { get; set; }

		/// <summary>
		/// Guild, notif settings
		/// </summary>
		[JsonProperty("default_message_notifications")]
		public GuildNotificationLevel? MessageNotifications { get; set; }

		/// <summary>
		/// Guild, vanity role
		/// </summary>
		[JsonProperty("vanity_url_code")]
		public string? VanityURL { get; set; }

		/// <summary>
		/// Guild, roles created
		/// </summary>
		[JsonProperty("$add")]
		public Role[]? AddedRoles { get; set; }

		/// <summary>
		/// Guild, roles removed
		/// </summary>
		[JsonProperty("$remove")]
		public Role[]? RemovedRoles { get; set; }

		/// <summary>
		/// Guild, age of pruned members
		/// </summary>
		[JsonProperty("prune_delete_days")]
		public int PruneDeleteDays { get; set; }

		/// <summary>
		/// Guild, widget state
		/// </summary>
		[JsonProperty("widget_enabled")]
		public bool? WidgetEnabled { get; set; }

		/// <summary>
		/// Guild, id of widget channel
		/// </summary>
		[JsonProperty("widget_channel_id")]
		public ulong? WidgetChannelID { get; set; }

		/// <summary>
		/// Guild, id of system channel
		/// </summary>
		[JsonProperty("system_channel_id")]
		public ulong? SystemChannelID { get; set; }

		/// <summary>
		/// Channel, position in list
		/// </summary>
		[JsonProperty("position")]
		public int? Position { get; set; }

		/// <summary>
		/// Channel, description
		/// </summary>
		[JsonProperty("topic")]
		public string? Topic { get; set; }

		/// <summary>
		/// Voice channel, bitrate
		/// </summary>
		[JsonProperty("bitrate")]
		public string? Bitrate { get; set; }

		/// <summary>
		/// Channel, custom permissions changed
		/// </summary>
		[JsonProperty("permission_overwrites")]
		public PermissionOverwrite[]? PermissionOverwrites { get; set; }

		/// <summary>
		/// Channel, nsfw state
		/// </summary>
		[JsonProperty("nsfw")]
		public bool? NSFW { get; set; }

		/// <summary>
		/// Channel, app ID of added/removed webhook or bot
		/// </summary>
		[JsonProperty("application_id")]
		public ulong? ApplicationID { get; set; }

		/// <summary>
		/// Channel, slow-mode
		/// </summary>
		[JsonProperty("rate_limit_per_user")]
		public int? SlowModeSpeed { get; set; }

		/// <summary>
		/// Role, permissions
		/// </summary>
		[JsonProperty("permissions")]
		public string? Permissions { get; set; }

		/// <summary>
		/// Role, color
		/// </summary>
		[JsonProperty("color")]
		public int? Color { get; set; }

		/// <summary>
		/// Role, display separately from other members
		/// </summary>
		[JsonProperty("hoist")]
		public bool? Hoist { get; set; }

		/// <summary>
		/// Role, mentionable
		/// </summary>
		[JsonProperty("mentionable")]
		public bool? Mentionable { get; set; }

		/// <summary>
		/// Role, this permission (number) was allowed for a role
		/// </summary>
		[JsonProperty("allow")]
		public string? Allowed { get; set; }

		/// <summary>
		/// Role, this permission (number) was denied for a role
		/// </summary>
		[JsonProperty("deny")]
		public string? Denied { get; set; }

		/// <summary>
		/// Invite, code
		/// </summary>
		[JsonProperty("code")]
		public string? Code { get; set; }

		/// <summary>
		/// Invite, target channel
		/// </summary>
		[JsonProperty("channel_id")]
		public ulong? ChannelID { get; set; }

		/// <summary>
		/// Invite, inviter ID
		/// </summary>
		[JsonProperty("inviter_id")]
		public ulong? InviterID { get; set; }

		/// <summary>
		/// Invite, max uses
		/// </summary>
		[JsonProperty("max_uses")]
		public int? MaxUses { get; set; }

		/// <summary>
		/// Invite, number of times used
		/// </summary>
		[JsonProperty("uses")]
		public int? Uses { get; set; }

		/// <summary>
		/// Invite, expiration time
		/// </summary>
		[JsonProperty("max_age")]
		public int? MaxAge { get; set; }

		/// <summary>
		/// Invite, grant temp. membership
		/// </summary>
		[JsonProperty("temporary")]
		public bool? TemporaryMembership { get; set; }

		/// <summary>
		/// User, server deafened
		/// </summary>
		[JsonProperty("deaf")]
		public bool? Deaf { get; set; }

		/// <summary>
		/// User, server muted
		/// </summary>
		[JsonProperty("mute")]
		public bool? Mute { get; set; }

		/// <summary>
		/// User, nickname
		/// </summary>
		[JsonProperty("nick")]
		public string? Nickname { get; set; }

		/// <summary>
		/// User, avatar
		/// </summary>
		[JsonProperty("avatar_hash")]
		public string? AvatarHash { get; set; }

		/// <summary>
		/// Universal. The ID of the thing that got changed
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// Universal. The type of thing that was created (int for channel type, or string)
		/// </summary>
		[JsonProperty("type")]
		public string Type { get; set; } = string.Empty;

		/// <summary>
		/// Integration: enable emotes
		/// </summary>
		[JsonProperty("enable_emoticons")]
		public bool? EnableEmoticons { get; set; }

		/// <summary>
		/// Integration: expiring subscriber behavior changed
		/// </summary>
		[JsonProperty("expire_behavior")]
		public int? ExpireBehavior { get; set; }

		/// <summary>
		/// Integration: Expiration grace period changed
		/// </summary>
		[JsonProperty("expire_grace_period")]
		public int? ExpireGracePeriod { get; set; }

	}
}

using EtiBotCore.Data;
using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a Discord Channel
	/// </summary>
	internal class Channel : PayloadDataObject {

		/// <summary>
		/// The ID of this channel.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// The type of channel that this is.
		/// </summary>
		[JsonProperty("type"), JsonConverter(typeof(EnumConverter))]
		public ChannelType Type { get; set; }

		/// <summary>
		/// The ID of the server that this channel is in, or <see langword="null"/> if this is a DM channel.
		/// </summary>
		[JsonProperty("guild_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? GuildID { get; set; }

		/// <summary>
		/// The position of this channel in the list, or <see langword="null"/> if this is a DM channel.
		/// </summary>
		[JsonProperty("position", NullValueHandling = NullValueHandling.Ignore)]
		public int? Position { get; set; }

		/// <summary>
		/// The permission overwrites this channel applies, or <see langword="null"/> if this is a DM channel.
		/// </summary>
		[JsonProperty("permission_overwrites", NullValueHandling = NullValueHandling.Ignore)]
		public PermissionOverwrite[]? PermissionOverwrites { get; set; }

		/// <summary>
		/// The name of this channel, or <see langword="null"/> if this is a DM channel. Can be anywhere from 2 to 100 characters.
		/// </summary>
		[JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
		public string? Name { get; set; }

		/// <summary>
		/// The topic or description of this channel, or <see langword="null"/> if this is a DM channel. Can be anywhere from 0 to 1024 characters.
		/// </summary>
		[JsonProperty("topic", NullValueHandling = NullValueHandling.Ignore)]
		public string? Topic { get; set; }

		/// <summary>
		/// Whether or not this channel is flagged as NSFW, or <see langword="null"/> if this is a DM channel.
		/// </summary>
		[JsonProperty("nsfw", NullValueHandling = NullValueHandling.Ignore)]
		public bool? NSFW { get; set; }

		/// <summary>
		/// The ID of the latest message. This may point to a message that no longer exists.
		/// </summary>
		[JsonProperty("last_message_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? LastMessageID { get; set; }

		/// <summary>
		/// The bitrate of the channel (in bits). Does not apply to DMs.
		/// </summary>
		[JsonProperty("bitrate", NullValueHandling = NullValueHandling.Ignore)]
		public int? Bitrate { get; set; }

		/// <summary>
		/// The maximum number of users in the channel. Does not apply to DMs.
		/// </summary>
		[JsonProperty("user_limit", NullValueHandling = NullValueHandling.Ignore)]
		public int? UserLimit { get; set; }

		/// <summary>
		/// The slow-mode duration timer on this channel in seconds. Bots are unaffected, as are users with the manage mesasges/channel permissions.
		/// </summary>
		[JsonProperty("rate_limit_per_user", NullValueHandling = NullValueHandling.Ignore)]
		public int? RateLimitPerUser { get; set; }

		/// <summary>
		/// The members of this DM.
		/// </summary>
		[JsonProperty("recipients", NullValueHandling = NullValueHandling.Ignore)]
		public User[]? Recipients { get; set; }

		/// <summary>
		/// The icon hash if this is a group DM. <see cref="Constants.UNSENT_STRING_DEFAULT"/> if this is not sent in the payload.
		/// </summary>
		[JsonProperty("icon", NullValueHandling = NullValueHandling.Ignore)]
		public string Icon { get; set; } = Constants.UNSENT_STRING_DEFAULT;

		/// <summary>
		/// If this is a group DM, this is the ID of the person who made it. If this is a thread, this is the creator of the thread.
		/// </summary>
		[JsonProperty("owner_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? OwnerID { get; set; }

		/// <summary>
		/// If this is a group DM, this is the ID of the application who made it if they are a bot.
		/// </summary>
		[JsonProperty("application_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? ApplicationID { get; set; }

		/// <summary>
		/// If this channel is in a category, this is the ID of the category object.
		/// </summary>
		[JsonProperty("parent_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? ParentID { get; set; }

		/// <summary>
		/// The timestamp of when the latest message was pinned.
		/// </summary>
		[JsonProperty("last_pin_timestamp"), JsonConverter(typeof(TimestampConverter))]
		public ISO8601? LastPinTimestamp { get; set; }

#pragma warning disable CS0169
#pragma warning disable IDE0051 // Remove unused private members
		private int message_count;
		private int member_count;
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CS0169

		/// <summary>
		/// The metadata for this thread, if this is a thread.
		/// </summary>
		[JsonProperty("thread_metadata")]
		public ThreadMetadata? Metadata { get; set; }

		/// <summary>
		/// For every thread message, a channel object is sent. This will be populated for messages received in a thread.
		/// Hell of a method. Yeah.
		/// So if this is a channel from a thread message, then this is the member.
		/// </summary>
		[JsonProperty("member")]
		public ThreadMember? Member { get; set; }

		/// <summary>
		/// The default auto-archive duration. May differ from that of <see cref="Metadata"/>, but is otherwise still restricted to being 60, 1440, 4320, or 10080.
		/// </summary>
		[JsonProperty("default_auto_archive_duration")]
		public int DefaultAutoArchiveDuration { get; set; }

		/// <summary>
		/// Only useful for slash commands, not implementing these yet.
		/// </summary>
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0169
		private string? permissions;
#pragma warning restore CS0169
#pragma warning restore IDE0051 // Remove unused private members

	}
}

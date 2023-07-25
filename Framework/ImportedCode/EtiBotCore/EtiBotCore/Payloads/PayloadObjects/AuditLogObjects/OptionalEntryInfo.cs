using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects.AuditLogObjects {
	internal class OptionalEntryInfo {

		/// <summary>
		/// The number of days after which inactive members were kicked.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.MEMBER_PRUNE"/>. It is a number as a string.
		/// </remarks>
		[JsonProperty("delete_member_days")]
		public string? DeleteMemberDays { get; set; } = string.Empty;

		/// <summary>
		/// The number of members removed by the prune.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.MEMBER_PRUNE"/>. It is a number as a string.
		/// </remarks>
		[JsonProperty("members_removed")]
		public string? MembersRemoved { get; set; } = string.Empty;

		/// <summary>
		/// The ID of the channel in which things were targeted.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.MEMBER_MOVE"/>, <see cref="AuditLogActionType.MESSAGE_PIN"/>, <see cref="AuditLogActionType.MESSAGE_UNPIN"/>, <see cref="AuditLogActionType.MESSAGE_DELETE"/>
		/// </remarks>
		[JsonProperty("channel_id")]
		public ulong? ChannelID { get; set; }

		/// <summary>
		/// The ID of the message that was targeted.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.MESSAGE_PIN"/>, <see cref="AuditLogActionType.MESSAGE_UNPIN"/>
		/// </remarks>
		[JsonProperty("message_id")]
		public ulong? MessageID { get; set; }

		/// <summary>
		/// The amount of entities that were targeted.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.MESSAGE_DELETE"/>, <see cref="AuditLogActionType.MESSAGE_BULK_DELETE"/>, <see cref="AuditLogActionType.MEMBER_DISCONNECT"/>, or <see cref="AuditLogActionType.MEMBER_MOVE"/>. It is a number as a string.
		/// </remarks>
		[JsonProperty("count")]
		public string? Count { get; set; }

		/// <summary>
		/// The ID of the thing that has its permissions changed (user or role)
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.CHANNEL_OVERWRITE_CREATE"/> (and update/delete). It is a number as a string.
		/// </remarks>
		[JsonProperty("id")]
		public string? ID { get; set; }

		/// <summary>
		/// The type of overwrite that it is.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.CHANNEL_OVERWRITE_CREATE"/> (and update/delete). It is a number as a string.<para/>
		/// It is 0 for a role and 1 for a member.
		/// </remarks>
		[JsonProperty("type")]
		public string? Type { get; set; }

		/// <summary>
		/// The name of the role that was changed.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.CHANNEL_OVERWRITE_CREATE"/> (and update/delete).<para/>
		/// Only present if <see cref="Type"/> is <c>"0"</c>, and null if <see cref="Type"/> is <c>"1"</c>
		/// </remarks>
		[JsonProperty("role_name")]
		public string? RoleName { get; set; }

	}
}

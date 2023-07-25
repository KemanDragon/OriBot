using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Data;

namespace EtiBotCore.DiscordObjects.Guilds.AuditLog {

	/// <summary>
	/// Optional data that might be present in an audit log entry.
	/// </summary>
	
	public class OptionalEntryInfo {


		/// <summary>
		/// The number of days after which inactive members were kicked.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.MEMBER_PRUNE"/>. It is a number as a string.
		/// </remarks>
		public int? DeleteMemberDays { get; private set; }

		/// <summary>
		/// The number of members removed by the prune.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.MEMBER_PRUNE"/>. It is a number as a string.
		/// </remarks>
		public int? MembersRemoved { get; private set; }

		/// <summary>
		/// The ID of the channel in which things were targeted.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.MEMBER_MOVE"/>, <see cref="AuditLogActionType.MESSAGE_PIN"/>, <see cref="AuditLogActionType.MESSAGE_UNPIN"/>, <see cref="AuditLogActionType.MESSAGE_DELETE"/>
		/// </remarks>
		public Snowflake? ChannelID { get; private set; }

		/// <summary>
		/// The ID of the message that was targeted.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.MESSAGE_PIN"/>, <see cref="AuditLogActionType.MESSAGE_UNPIN"/>
		/// </remarks>
		public Snowflake? MessageID { get; private set; }

		/// <summary>
		/// The amount of entities that were targeted.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.MESSAGE_DELETE"/>, <see cref="AuditLogActionType.MESSAGE_BULK_DELETE"/>, <see cref="AuditLogActionType.MEMBER_DISCONNECT"/>, or <see cref="AuditLogActionType.MEMBER_MOVE"/>. It is a number as a string.
		/// </remarks>
		public int? Count { get; private set; }

		/// <summary>
		/// The ID of the thing that has its permissions changed (user or role)
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.CHANNEL_OVERWRITE_CREATE"/> (and update/delete). It is a number as a string.
		/// </remarks>
		public Snowflake? ID { get; private set; }

		/// <summary>
		/// The type of overwrite that it is.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.CHANNEL_OVERWRITE_CREATE"/> (and update/delete). It is a number as a string.<para/>
		/// It is 0 for a role and 1 for a member.
		/// </remarks>
		public PermissionOverwriteTargetType? Type { get; private set; }

		/// <summary>
		/// The name of the role that was changed.
		/// </summary>
		/// <remarks>
		/// Only present if the type is <see cref="AuditLogActionType.CHANNEL_OVERWRITE_CREATE"/> (and update/delete).<para/>
		/// Only present if <see cref="Type"/> is <c>"0"</c>, and null if <see cref="Type"/> is <c>"1"</c>
		/// </remarks>
		public string? RoleName { get; private set; }

		internal static OptionalEntryInfo? FromPayload(Payloads.PayloadObjects.AuditLogObjects.OptionalEntryInfo? info) {
			if (info == null) return null;

			OptionalEntryInfo newInfo = new OptionalEntryInfo();
			if (info.DeleteMemberDays != null && int.TryParse(info.DeleteMemberDays, out int deleteMemberDays)) {
				newInfo.DeleteMemberDays = deleteMemberDays;
			}
			if (info.MembersRemoved != null && int.TryParse(info.MembersRemoved, out int membersRemoved)) {
				newInfo.MembersRemoved = membersRemoved;
			}
			if (info.ChannelID != null) {
				newInfo.ChannelID = info.ChannelID!.Value;
			}
			if (info.MessageID != null) {
				newInfo.MessageID = info.MessageID!.Value;
			}
			if (info.Count != null && int.TryParse(info.Count, out int count)) {
				newInfo.Count = count;
			}
			if (info.ID != null && Snowflake.TryParse(info.ID, out Snowflake id)) {
				newInfo.ID = id;
			}
			if (info.Type != null && int.TryParse(info.Type, out int type)) {
				newInfo.Type = (PermissionOverwriteTargetType)type;
			}
			if (!string.IsNullOrWhiteSpace(info.RoleName)) {
				newInfo.RoleName = info.RoleName;
			}
			return newInfo;
		}

	}
}

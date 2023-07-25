using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Data;

namespace EtiBotCore.DiscordObjects.Guilds.AuditLog {

	/// <summary>
	/// An entry in the audit log.
	/// </summary>
	
	public class AuditLogEntry {

		/// <summary>
		/// The ID of the affected entity (user, role, etc.)
		/// </summary>
		public Snowflake TargetID { get; private set; }

		/// <summary>
		/// The changes made in this entry.
		/// </summary>
		public AuditLogChangeContainer[]? Changes { get; private set; }

		/// <summary>
		/// The user ID of whoever did the thingy.
		/// </summary>
		public Snowflake UserID { get; private set; }

		/// <summary>
		/// The ID of this entry itself.
		/// </summary>
		public Snowflake ID { get; private set; }

		/// <summary>
		/// The type of action that occurred.
		/// </summary>
		public AuditLogActionType ActionType { get; private set; }

		/// <summary>
		/// Additional info for certain specific action types.
		/// </summary>
		public OptionalEntryInfo? Options { get; private set; }

		/// <summary>
		/// The reason this action was performed.
		/// </summary>
		public string? Reason { get; private set; }


		internal static AuditLogEntry FromPayload(Payloads.PayloadObjects.AuditLogObjects.AuditLogEntry entry) {
			AuditLogChangeContainer[]? changes = null;
			if (entry.Changes != null) {
				changes = new AuditLogChangeContainer[entry.Changes.Length];
				for (int idx = 0; idx < changes.Length; idx++) {
					changes[idx] = AuditLogChangeContainer.CreateAuto(entry.Changes[idx]);
				}
			}
			AuditLogEntry newEnt = new AuditLogEntry {
				TargetID = entry.TargetID ?? 0,
				UserID = entry.UserID,
				ID = entry.ID,
				ActionType = entry.ActionType,
				Reason = entry.Reason,
				Options = OptionalEntryInfo.FromPayload(entry.Options),
				Changes = changes
			};
			return newEnt;
		}

	}
}

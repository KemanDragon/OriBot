using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects.AuditLogObjects {

	/// <summary>
	/// An entry in the audit log.
	/// </summary>
	internal class AuditLogEntry {

		/// <summary>
		/// The ID of the affected entity (user, role, etc.)
		/// </summary>
		[JsonProperty("target_id")]
		public ulong? TargetID { get; set; }

		/// <summary>
		/// The changes made in this entry.
		/// </summary>
		[JsonProperty("changes")]
		public AuditLogChange[]? Changes { get; set; }

		/// <summary>
		/// The user ID of whoever did the thingy.
		/// </summary>
		[JsonProperty("user_id")]
		public ulong UserID { get; set; }

		/// <summary>
		/// The ID of this entry itself.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// The type of action that occurred.
		/// </summary>
		[JsonProperty("action_type")]
		public AuditLogActionType ActionType { get; set; }

		/// <summary>
		/// Additional info for certain specific action types.
		/// </summary>
		[JsonProperty("options")]
		public OptionalEntryInfo? Options { get; set; }

		/// <summary>
		/// The reason this action was performed.
		/// </summary>
		[JsonProperty("reason")]
		public string? Reason { get; set; }

	}
}

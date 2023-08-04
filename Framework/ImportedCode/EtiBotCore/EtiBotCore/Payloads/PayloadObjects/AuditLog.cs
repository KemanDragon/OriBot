using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads.PayloadObjects.AuditLogObjects;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// A server's audit log.
	/// </summary>
	internal class AuditLog : PayloadDataObject {

		/// <summary>
		/// The webhooks found in the audit log.
		/// </summary>
		[JsonProperty("webhooks")]
		public Webhook[] Webhooks { get; set; } = new Webhook[0];

		/// <summary>
		/// The users found in the audit log.
		/// </summary>
		[JsonProperty("users")]
		public User[] Users { get; set; } = new User[0];

		/// <summary>
		/// The entries in the audit log.
		/// </summary>
		[JsonProperty("audit_log_entries")]
		public AuditLogEntry[] Entries { get; set; } = new AuditLogEntry[0];

		/// <summary>
		/// The list of partial integrations in this log.
		/// </summary>
		[JsonProperty("integrations")]
		public PartialIntegration[] Integrations { get; set; } = new PartialIntegration[0];

	}
}

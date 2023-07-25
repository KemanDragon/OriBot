#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using EtiBotCore.DiscordObjects.Universal;

namespace EtiBotCore.DiscordObjects.Guilds.AuditLog {

	/// <summary>
	/// Represents an entry in a server's audit log.
	/// </summary>
	
	public class AuditLogObject {

		/// <summary>
		/// The server this entry exists for.
		/// </summary>
		public Guild Server { get; private set; }

		/// <summary>
		/// The users in this audit log.
		/// </summary>
		public IReadOnlyList<User> Users { get; private set; }

		/// <summary>
		/// The entries in the audit log.
		/// </summary>
		public IReadOnlyList<AuditLogEntry> Entries { get; private set; }

		/// <summary>
		/// The webhooks in the audit log.
		/// </summary>
		[Obsolete("This is not implemented.", true)] public object? Webhooks { get => throw new NotImplementedException(); }

		/// <summary>
		/// The Integrations in the audit log.
		/// </summary>
		[Obsolete("This is not implemented.", true)] public object? Integrations { get => throw new NotImplementedException(); }

		internal static AuditLogObject FromPayload(Guild origin, Payloads.PayloadObjects.AuditLog audit) {
			List<User> users = new List<User>();
			List<AuditLogEntry> entries = new List<AuditLogEntry>();

			foreach (var plUser in audit.Users) {
				users.Add(User.EventGetOrCreate(plUser));
			}
			foreach (var entry in audit.Entries) {
				entries.Add(AuditLogEntry.FromPayload(entry));
			}

			return new AuditLogObject {
				Server = origin,
				Users = users,
				Entries = entries,
				//Webhooks = audit.Webhooks,
				//Integrations = audit.Integrations
			};
		}

	}
}

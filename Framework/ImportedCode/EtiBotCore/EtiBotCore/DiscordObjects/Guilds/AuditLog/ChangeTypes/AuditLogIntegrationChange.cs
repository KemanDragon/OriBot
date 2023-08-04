using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.DiscordObjects.Guilds.AuditLog.ChangeTypes {

	/// <summary>
	/// A change made to an integration in the audit log.
	/// </summary>
	
	public class AuditLogIntegrationChange : AbstractAuditLogChangeBase {

		/// <summary>
		/// The new state of whether or not this integration has emojis, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public bool? EnableEmoticons { get; }

		/// <summary>
		/// The new state of subscriber expiration behavior, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public int? ExpireBehavior { get; }

		/// <summary>
		/// The new duration of the grace period for expired subscribers, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public int? ExpireGracePeriod { get; }

		internal AuditLogIntegrationChange(Payloads.PayloadObjects.AuditLogObjects.AuditLogChange sourceChange) : base(sourceChange.ID, sourceChange.Type) {
			EnableEmoticons = sourceChange.EnableEmoticons;
			ExpireBehavior = sourceChange.ExpireBehavior;
			ExpireGracePeriod = sourceChange.ExpireGracePeriod;
		}

	}
}

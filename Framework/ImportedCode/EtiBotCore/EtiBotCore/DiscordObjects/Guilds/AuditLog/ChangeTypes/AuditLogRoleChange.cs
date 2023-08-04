using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads.Data;

namespace EtiBotCore.DiscordObjects.Guilds.AuditLog.ChangeTypes {

	/// <summary>
	/// A change made to a role in the audit log
	/// </summary>
	
	public class AuditLogRoleChange : AbstractAuditLogChangeBase {

		/// <summary>
		/// The new permissions of this role, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public Permissions Permissions { get; }

		/// <summary>
		/// The new color of this role, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public int? Color { get; }

		/// <summary>
		/// The new state of whether or not to display this role separately in the list, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public bool? Hoist { get; }

		/// <summary>
		/// The new state of the role being mentionable, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public bool? Mentionable { get; }

		/// <summary>
		/// The permissions the role is allowed, if it was a channel that changed, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public Permissions? Allowed { get; }

		/// <summary>
		/// The permissions the role is denied, if it was a channel that changed, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public Permissions? Denied { get; }

		internal AuditLogRoleChange(Payloads.PayloadObjects.AuditLogObjects.AuditLogChange changeSource) : base(changeSource.ID, changeSource.Type) {
			if (changeSource.Permissions != null) Permissions = (Permissions)int.Parse(changeSource.Permissions);
			Color = changeSource.Color;
			Hoist = changeSource.Hoist;
			Mentionable = changeSource.Mentionable;
			if (changeSource.Allowed != null) Allowed = (Permissions)int.Parse(changeSource.Allowed);
			if (changeSource.Denied != null) Denied = (Permissions)int.Parse(changeSource.Denied);

		}

	}
}

using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.DiscordObjects.Guilds.AuditLog.ChangeTypes {

	/// <summary>
	/// A change made to a user in the audit log.
	/// </summary>
	
	public class AuditLogUserChange : AbstractAuditLogChangeBase {

		/// <summary>
		/// The new nickname, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public string? Nickname { get; }

		/// <summary>
		/// The new state of this user being server deafened, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public bool? ServerDeafened { get; }

		/// <summary>
		/// The new state of this user being server muted, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public bool? ServerMuted { get; }

		/// <summary>
		/// The user's new avatar, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public string? AvatarHash { get; }


		internal AuditLogUserChange(Payloads.PayloadObjects.AuditLogObjects.AuditLogChange changeSource) : base(changeSource.ID, changeSource.Type) {
			Nickname = changeSource.Nickname;
			ServerDeafened = changeSource.Deaf;
			ServerMuted = changeSource.Mute;
			AvatarHash = changeSource.AvatarHash;
		}
	}
}

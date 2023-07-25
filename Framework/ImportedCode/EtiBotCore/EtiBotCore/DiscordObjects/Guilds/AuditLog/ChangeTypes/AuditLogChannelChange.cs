using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Payloads.Data;

namespace EtiBotCore.DiscordObjects.Guilds.AuditLog.ChangeTypes {

	/// <summary>
	/// A change made to a channel in the audit log.
	/// </summary>
	
	public class AuditLogChannelChange : AbstractAuditLogChangeBase {

		/// <summary>
		/// The new position of this channel in the list (where 0 is first, top), or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public int? Position { get; }

		/// <summary>
		/// The new channel description, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public string? Topic { get; }

		/// <summary>
		/// The new channel bitrate, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public int? Bitrate { get; }

		/// <summary>
		/// The new permissions for this channel, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		/// <remarks>
		/// This dictionary is a mapping from user/role ID to a pair of permissions. This tuple is mapped out as (allow, deny). Any permissions that are in neither of the two groups are inherited permissions.
		/// </remarks>
		public Dictionary<Snowflake, (Permissions, Permissions)>? Permissions { get; }

		/// <summary>
		/// The new NSFW status of this channel, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public bool? NSFW { get; }

		/// <summary>
		/// The application ID of an added/removed webhook or bot, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public Snowflake? ApplicationID { get; }

		/// <summary>
		/// The new amount of seconds a user has to wait between sending messages, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public int? SlowModeTimer { get; }

		internal AuditLogChannelChange(Payloads.PayloadObjects.AuditLogObjects.AuditLogChange changeSource) : base(changeSource.ID, changeSource.Type) {
			Position = changeSource.Position;
			Topic = changeSource.Topic;
			if (changeSource.Bitrate != null) Bitrate = int.Parse(changeSource.Bitrate);
			
			if (changeSource.PermissionOverwrites != null) {
				Permissions = new Dictionary<Snowflake, (Permissions, Permissions)>();
				foreach (Payloads.PayloadObjects.PermissionOverwrite overwrite in changeSource.PermissionOverwrites) {
					Permissions[overwrite.ID] = (overwrite.AllowPermissions, overwrite.DenyPermissions);
				}
			}

			NSFW = changeSource.NSFW;
			ApplicationID = changeSource.ApplicationID;
			SlowModeTimer = changeSource.SlowModeSpeed;
		}
	}
}

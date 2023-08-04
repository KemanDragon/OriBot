using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Data;

namespace EtiBotCore.DiscordObjects.Guilds.AuditLog.ChangeTypes {

	/// <summary>
	/// A change made to a guild in the audit log.
	/// </summary>
	
	public class AuditLogGuildChange : AbstractAuditLogChangeBase {

		/// <summary>
		/// The new name of this guild, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public string? Name { get; }

		/// <summary>
		/// The new hash of the guild's icon, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public string? IconHash { get; }

		/// <summary>
		/// The new hash of the guild's splash, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public string? SplashHash { get; }

		/// <summary>
		/// The ID of the new server owner, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public Snowflake? OwnerID { get; }

		/// <summary>
		/// The new voice region, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public string? Region { get; }

		/// <summary>
		/// The new AFK channel's ID, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public Snowflake? AFKChannelID { get; }

		/// <summary>
		/// The new AFK timeout in seconds, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public int? AFKTimeout { get; }

		/// <summary>
		/// The new MFA level, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public MFALevel? MFALevel { get; }

		/// <summary>
		/// The new verification level, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public VerificationLevel? VerificationLevel { get; }

		/// <summary>
		/// The new explicit content filter level, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public ExplicitContentFilterLevel? ExplicitFilterLevel { get; }

		/// <summary>
		/// The new default message notification level, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public GuildNotificationLevel? DefaultMessageNotifications { get; }

		/// <summary>
		/// The new vanity URL, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public string? VanityURL { get; }

		/// <summary>
		/// The roles that were added to the server, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public PartialRoleEntry[]? AddedRoles { get; }

		/// <summary>
		/// The roles that were removed from the server, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public PartialRoleEntry[]? RemovedRoles { get; }

		/// <summary>
		/// The amount of days of inactivity from which members were pruned, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public int? PruneDeleteDays { get; }

		/// <summary>
		/// Whether or not the widget was enabled or disabled, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public bool? WidgetEnabled { get; }

		/// <summary>
		/// The new ID of the widget channel, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public Snowflake? WidgetChannelID { get; }

		/// <summary>
		/// The new ID of the system channel, or <see langword="null"/> if it wasn't changed.
		/// </summary>
		public Snowflake? SystemChannelID { get; }

		internal AuditLogGuildChange(Payloads.PayloadObjects.AuditLogObjects.AuditLogChange changeSource) : base(changeSource.ID, changeSource.Type) {
			Name = changeSource.Name;
			IconHash = changeSource.IconHash;
			SplashHash = changeSource.SplashHash;
			OwnerID = changeSource.OwnerID;
			Region = changeSource.Region;
			AFKChannelID = changeSource.AFKChannelID;
			AFKTimeout = changeSource.AFKTimeout;
			MFALevel = changeSource.MFALevel;
			VerificationLevel = changeSource.VerificationLevel;
			ExplicitFilterLevel = changeSource.ExplicitFilterLevel;
			DefaultMessageNotifications = changeSource.MessageNotifications;
			VanityURL = changeSource.VanityURL;
			if (changeSource.AddedRoles != null) {
				AddedRoles = new PartialRoleEntry[changeSource.AddedRoles.Length];
				for (int idx = 0; idx < AddedRoles.Length; idx++) {
					string name = changeSource.AddedRoles[idx].Name;
					Snowflake id = changeSource.AddedRoles[idx].ID;
					AddedRoles[idx] = new PartialRoleEntry(id, name);
				}
			}

			if (changeSource.RemovedRoles != null) {
				RemovedRoles = new PartialRoleEntry[changeSource.RemovedRoles.Length];
				for (int idx = 0; idx < RemovedRoles.Length; idx++) {
					string name = changeSource.RemovedRoles[idx].Name;
					Snowflake id = changeSource.RemovedRoles[idx].ID;
					RemovedRoles[idx] = new PartialRoleEntry(id, name);
				}
			}

			PruneDeleteDays = changeSource.PruneDeleteDays;
			WidgetEnabled = changeSource.WidgetEnabled;
			WidgetChannelID = changeSource.WidgetChannelID;
			SystemChannelID = changeSource.SystemChannelID;
		}

		/// <summary>
		/// A partial role, containing only a name and ID.
		/// </summary>
		public class PartialRoleEntry {

			/// <summary>
			/// The ID of this role.
			/// </summary>
			public Snowflake ID { get; }

			/// <summary>
			/// The name of this role.
			/// </summary>
			public string Name { get; }

			/// <summary>
			/// Construct a new <see cref="PartialRoleEntry"/> wit hthe given data.
			/// </summary>
			/// <param name="id"></param>
			/// <param name="name"></param>
			public PartialRoleEntry(Snowflake id, string name) {
				ID = id;
				Name = name;
			}

		}
	}
}

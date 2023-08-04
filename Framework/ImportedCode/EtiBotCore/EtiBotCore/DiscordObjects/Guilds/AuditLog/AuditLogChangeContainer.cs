using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.DiscordObjects.Guilds.AuditLog.ChangeTypes;

namespace EtiBotCore.DiscordObjects.Guilds.AuditLog {

	/// <summary>
	/// A container type for the various types of audit log changes.
	/// </summary>
	
	public class AuditLogChangeContainer {

		/// <summary>
		/// The changes made to the guild.
		/// </summary>
		public AuditLogGuildChange GuildChanges { get; }

		/// <summary>
		/// The changes made to a channel.
		/// </summary>
		public AuditLogChannelChange ChannelChanges { get; }

		/// <summary>
		/// The changes made to a role or its permissions.
		/// </summary>
		public AuditLogRoleChange RoleChanges { get; }

		/// <summary>
		/// The changes made to a user.
		/// </summary>
		public AuditLogUserChange UserChanges { get; }

		/// <summary>
		/// The changes made to an integration.
		/// </summary>
		public AuditLogIntegrationChange IntegrationChanges { get; }

		/// <summary>
		/// Construct a new change container.
		/// </summary>
		/// <param name="guild"></param>
		/// <param name="channel"></param>
		/// <param name="role"></param>
		/// <param name="user"></param>
		/// <param name="integration"></param>
		private AuditLogChangeContainer(AuditLogGuildChange guild, AuditLogChannelChange channel, AuditLogRoleChange role, AuditLogUserChange user, AuditLogIntegrationChange integration) {
			GuildChanges = guild;
			ChannelChanges = channel;
			RoleChanges = role;
			UserChanges = user;
			IntegrationChanges = integration;
		}

		/// <summary>
		/// Create a new container by constructing change objects from the given payload.
		/// </summary>
		/// <param name="plChange"></param>
		/// <returns></returns>
		internal static AuditLogChangeContainer CreateAuto(Payloads.PayloadObjects.AuditLogObjects.AuditLogChange plChange) {
			return new AuditLogChangeContainer(
				new AuditLogGuildChange(plChange),
				new AuditLogChannelChange(plChange),
				new AuditLogRoleChange(plChange),
				new AuditLogUserChange(plChange),
				new AuditLogIntegrationChange(plChange)
			);
		}

	}
}

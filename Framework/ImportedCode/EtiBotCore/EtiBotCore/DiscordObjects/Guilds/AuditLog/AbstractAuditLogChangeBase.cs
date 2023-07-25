using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds.AuditLog.ChangeTypes;
using EtiBotCore.Payloads.Data;

namespace EtiBotCore.DiscordObjects.Guilds.AuditLog {

	/// <summary>
	/// The base class for representing changes made to something in the audit log.
	/// </summary>
	
	public abstract class AbstractAuditLogChangeBase {

		/// <summary>
		/// The ID of the thing that got changed.
		/// </summary>
		public Snowflake ID { get; }

		/// <summary>
		/// If <see cref="TypeOfThingChanged"/> is <c>channel</c>, this is the type of channel.
		/// </summary>
		public ChannelType? ChannelType { get; }

		/// <summary>
		/// The type of the thing that got changed. Either <c>channel, role, user, integration, guild</c>
		/// </summary>
		public string TypeOfThingChanged { get; }

		/// <summary>
		/// Construct a <see cref="AbstractAuditLogChangeBase"/> from the given common data.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="changeType"></param>
		public AbstractAuditLogChangeBase(Snowflake id, string changeType) {
			ID = id;
			if (int.TryParse(changeType, out int changeId)) {
				ChannelType = (ChannelType)changeId;
				TypeOfThingChanged = "channel";
			} else {
				TypeOfThingChanged = changeType;
			}
		}

	}
}

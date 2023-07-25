#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using SignalCore;

namespace EtiBotCore.Client.EventContainers {

	/// <summary>
	/// An event container storing events that fire when an instant invite is created or deleted.
	/// </summary>
	public class EventContainerInvites {

		internal EventContainerInvites() { }

		/// <summary>
		/// Fires when an instant invite is created for a server. If this is a group DM invite, the guild argument in the <see cref="Invite"/> will be <see langword="null"/>.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>newInvite</c>
		/// </remarks>
		public Signal<Invite> OnInviteCreated { get; set; } = new Signal<Invite>();

		/// <summary>
		/// Fires when an instant invite is deleted from a server or group DM. If this is a group DM invite, the guild argument will be <see langword="null"/>.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>serverId, channelId, inviteCode</c>
		/// </remarks>
		public Signal<Snowflake?, Snowflake, string> OnInviteDeleted { get; set; } = new Signal<Snowflake?, Snowflake, string>();

	}
}

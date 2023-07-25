#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Universal;
using SignalCore;

namespace EtiBotCore.Client.EventContainers {

	/// <summary>
	/// An event container pertaining to when a member is banned or unbanned.
	/// </summary>
	public class EventContainerBans {

		internal EventContainerBans() { }

		/// <summary>
		/// Fires when a member is banned from a server.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, user</c>
		/// </remarks>
		public Signal<Guild, User> OnMemberBanned { get; set; } = new Signal<Guild, User>();

		/// <summary>
		/// Fires when a member is unbanned from a server.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, user</c>
		/// </remarks>
		public Signal<Guild, User> OnMemberUnbanned { get; set; } = new Signal<Guild, User>();

	}
}

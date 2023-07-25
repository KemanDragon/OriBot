#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Universal;
using SignalCore;

namespace EtiBotCore.Client.EventContainers {

	/// <summary>
	/// An event that fires when integrations of the server update.
	/// </summary>
	public class EventContainerIntegrations {

		internal EventContainerIntegrations() { }

		/// <summary>
		/// Fires when the integrations to the server update.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server</c>
		/// </remarks>
		public Signal<Guild> OnIntegrationsUpdated { get; set; } = new Signal<Guild>();

	}
}

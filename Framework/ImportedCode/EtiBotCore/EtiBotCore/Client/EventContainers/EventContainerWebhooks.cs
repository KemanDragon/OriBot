#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Universal;
using SignalCore;

namespace EtiBotCore.Client.EventContainers {

	/// <summary>
	/// An event container for webhook changes, creations, or deletions.
	/// </summary>
	public class EventContainerWebhooks {

		internal EventContainerWebhooks() { }

		/// <summary>
		/// An event that fires when the webhooks associated with a given channel update.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, channel</c>
		/// </remarks>
		public Signal<Guild, GuildChannelBase> OnWebhooksUpdated { get; set; } = new Signal<Guild, GuildChannelBase>();

	}
}

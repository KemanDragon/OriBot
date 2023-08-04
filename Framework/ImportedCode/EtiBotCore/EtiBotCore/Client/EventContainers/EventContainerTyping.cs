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
	/// An event container storing an event that fires when someone starts typing in a text channel.
	/// </summary>
	public class EventContainerTyping {

		internal EventContainerTyping() { }

		/// <summary>
		/// Fires when a user starts typing in a channel.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>user, inChannel, atTime</c>
		/// </remarks>
		public Signal<User, ChannelBase, DateTimeOffset> OnTypingStarted { get; set; } = new Signal<User, ChannelBase, DateTimeOffset>();

	}
}

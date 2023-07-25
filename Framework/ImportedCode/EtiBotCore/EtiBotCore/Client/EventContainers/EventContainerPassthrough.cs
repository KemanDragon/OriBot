using EtiBotCore.Data.Structs;
using SignalCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Client.EventContainers {

	/// <summary>
	/// Contains all events that are considered passthrough (the bot will receive these no matter what, even if it has no intents defined).
	/// </summary>
	/// <remarks>
	/// This class is purposely left incomplete as the other passthrough events are only relevant to the bot core.
	/// </remarks>
	public class EventContainerPassthrough {

		internal EventContainerPassthrough() { }

		/// <summary>
		/// An event that fires when the bot is instructed to connect to a voice server.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>serverId, token, endpoint</c><para/>
		/// A <see langword="null"/> endpoint means that the voice server allocated has gone away and is trying to be reallocated. You should attempt to disconnect from the currently connected voice server, and not attempt to reconnect until a new voice server is allocated.
		/// </remarks>
		public Signal<Snowflake, string, string?> OnVoiceServerUpdated = new Signal<Snowflake, string, string?>();

	}
}

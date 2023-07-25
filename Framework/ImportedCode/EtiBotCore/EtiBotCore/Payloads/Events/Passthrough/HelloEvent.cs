using EtiBotCore.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Passthrough {

	/// <summary>
	/// The "Hello" payload, which stores the heartbeat interval.
	/// </summary>
	internal class HelloEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The interval in which Discord expects heartbeats.
		/// </summary>
		[JsonProperty("heartbeat_interval")]
		public int HeartbeatInterval { get; set; }

		public Task Execute(DiscordClient fromClient) => Task.CompletedTask; // handled explicitly
	}
}

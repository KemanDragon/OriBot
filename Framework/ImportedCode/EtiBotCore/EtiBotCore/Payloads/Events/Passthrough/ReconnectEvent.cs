using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Payloads.Commands;

namespace EtiBotCore.Payloads.Events.Passthrough {

	/// <summary>
	/// The server this bot is connected to is going away. The client should reconnect to the gateway and send a <see cref="ResumeCommand"/>.
	/// </summary>
	internal class ReconnectEvent : PayloadDataObject, IEvent {
		public Task Execute(DiscordClient fromClient) => Task.CompletedTask; // handled explicitly
	}

}

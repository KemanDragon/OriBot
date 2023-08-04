using EtiBotCore.Client;
using EtiBotCore.Payloads.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EtiBotCore.Payloads.Events.Passthrough {

	/// <summary>
	/// A response to a <see cref="ResumeCommand"/>.
	/// </summary>
	internal class ResumedEvent : PayloadDataObject, IEvent {
		public Task Execute(DiscordClient fromClient) => Task.CompletedTask; // handled explicitly
	}
}

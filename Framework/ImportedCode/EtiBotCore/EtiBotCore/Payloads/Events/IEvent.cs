using EtiBotCore.Client;
using EtiBotCore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable disable
namespace EtiBotCore.Payloads.Events {

	/// <summary>
	/// An empty interface that signifies this class is an event that can be received from Discord. This is also used to register it in <see cref="PayloadEventRegistry"/>.
	/// </summary>
	public interface IEvent {

		/// <summary>
		/// Returns the event name associated with this <see cref="IEvent"/>.
		/// </summary>
		string GetEventName();

		/// <summary>
		/// Executes any code that this event should perform.
		/// </summary>
		Task Execute(DiscordClient fromClient);

	}
}

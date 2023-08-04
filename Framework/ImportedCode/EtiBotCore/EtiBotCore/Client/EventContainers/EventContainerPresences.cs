#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds.MemberData;
using SignalCore;

namespace EtiBotCore.Client.EventContainers {

	/// <summary>
	/// An event container storing an event that fires when someone's presence (e.g. online, away, etc.) changes.
	/// </summary>
	public class EventContainerPresences {

		internal EventContainerPresences() { }

		/// <summary>
		/// An event that fires when a member's presence is updated. The presence object contains the corresponding member ID. The old presence may be null.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>presenceBefore, presenceAfter</c>
		/// </remarks>
		public Signal<Presence, Presence> OnPresenceUpdated { get; set; } = new Signal<Presence, Presence>();

	}
}

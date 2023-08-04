#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Universal;
using SignalCore;

namespace EtiBotCore.Client.EventContainers {

	/// <summary>
	/// An event container for events pertaining to when emojis in a server change.
	/// </summary>
	public class EventContainerEmojis {

		internal EventContainerEmojis() { }

		/// <summary>
		/// Fires when the emojis in a server update. Both emoji arrays are CustomEmoji objects.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, emojisBefore, emojisAfter</c>
		/// </remarks>
		public Signal<Guild, Emoji[], Emoji[]> OnEmojisUpdated { get; set; } = new Signal<Guild, Emoji[], Emoji[]>();

	}
}

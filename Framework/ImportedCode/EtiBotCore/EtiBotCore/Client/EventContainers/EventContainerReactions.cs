#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using SignalCore;

namespace EtiBotCore.Client.EventContainers {

	/// <summary>
	/// A container for reaction events.
	/// </summary>
	public class EventContainerReactions {

		/// <summary>
		/// An event container storing information for when a reaction is added or removed.
		/// </summary>
		internal EventContainerReactions() { }

		/// <summary>
		/// An event that fires when a reaction is added to a message.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>onMessage, emoji, reactor</c>
		/// </remarks>
		public Signal<Message, Emoji, User> OnReactionAdded { get; set; } = new Signal<Message, Emoji, User>();

		/// <summary>
		/// An event that fires when a reaction is removed from a message.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>onMessage, emoji, reactor</c>
		/// </remarks>
		public Signal<Message, Emoji, User> OnReactionRemoved { get; set; } = new Signal<Message, Emoji, User>();

		/// <summary>
		/// An event that fires when all reactions are removed from a message.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>onMessage</c>
		/// </remarks>
		public Signal<Message> OnAllReactionsRemoved { get; set; } = new Signal<Message>();

		/// <summary>
		/// An event that fires when all reactions of a specific emoji are removed from a message.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>onMessage, emoji</c>
		/// </remarks>
		public Signal<Message, Emoji> OnAllReactionsOfEmojiRemoved { get; set; } = new Signal<Message, Emoji>();

	}
}

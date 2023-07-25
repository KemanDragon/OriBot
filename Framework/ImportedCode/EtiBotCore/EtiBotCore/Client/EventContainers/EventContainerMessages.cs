#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Threading;
using SignalCore;

namespace EtiBotCore.Client.EventContainers {

	/// <summary>
	/// Contains events pertaining to messages being sent, edited, or deleted.
	/// </summary>
	public class EventContainerMessages {

		/// <summary>
		/// Messages that have been part of a bulk delete event.
		/// </summary>
		internal readonly List<Snowflake> BulkMessages = new List<Snowflake>();

		/// <summary>
		/// An event that fires when a message is sent.<para/>
		/// The pinned argument will be true or false to signify whether or not the message is pinned, and null if it was not sent and is therefore unknown.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>message, isPinned</c>
		/// </remarks>
		public Signal<Message, bool?> OnMessageCreated { get; set; } = new Signal<Message, bool?>();

		/// <summary>
		/// An event that fires when a message is edited.<para/>
		/// The pinned argument will be true or false to signify whether or not the message's pinned status changed (it was pinned or unpinned). It will be null if there was no change.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>messageBefore, messageAfter, pinStateIfChanged</c>
		/// </remarks>
		public Signal<Message, Message, bool?> OnMessageEdited { get; set; } = new Signal<Message, Message, bool?>();

		/// <summary>
		/// An event that fires when an individual message is deleted. Messages deleted in a bulk operation will not fire this event.<para/>
		/// This includes the message's ID. Depending on what the bot has seen, the message may still exist in memory, albeit with <see cref="DiscordObjects.DiscordObject.Deleted"/> = <see langword="true"/>
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>messageId, inChannel</c>
		/// </remarks>
		public Signal<Snowflake, ChannelBase> OnMessageDeleted { get; set; } = new Signal<Snowflake, ChannelBase>();

		/// <summary>
		/// An event that fires when several messages are deleted at once.<para/>
		/// This includes the messages' IDs. Depending on what the bot has seen, the messages may still exist in memory, albeit with <see cref="DiscordObjects.DiscordObject.Deleted"/> = <see langword="true"/>
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>messageIds, inChannel</c>
		/// </remarks>
		public Signal<Snowflake[], ChannelBase> OnMessagesBulkDeleted { get; set; } = new Signal<Snowflake[], ChannelBase>();

		/// <summary>
		/// An event that fires if the <see cref="GatewayIntent.DIRECT_MESSAGES"/> intent is active for when a message is pinned in a DM. <strong>This will never fire for pins in a server.</strong><para/>
		/// For messages being pinned in servers, see <see cref="EventContainerGuilds"/> (<see cref="DiscordClient.EventContainer.GuildEvents"/>).
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>dmChannel</c>
		/// </remarks>
		public Signal<DMChannel> OnDirectMessagePinStateChanged { get; set; } = new Signal<DMChannel>();

	}
}

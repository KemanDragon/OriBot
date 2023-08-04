using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.Data {
	
	/// <summary>
	/// Represents the type of message that something is.
	/// </summary>
	public enum MessageType {

		/// <summary>
		/// This is a standard message.
		/// </summary>
		Default = 0,

		/// <summary>
		/// A recipient was added to this DM.
		/// </summary>
		RecipientAdded = 1,

		/// <summary>
		/// A recipient was removed from this DM.
		/// </summary>
		RecipientRemoved = 2,
		
		/// <summary>
		/// A call has started in this DM.
		/// </summary>
		Call = 3,

		/// <summary>
		/// The name of the DM group has been changed.
		/// </summary>
		ChannelNameChange = 4,

		/// <summary>
		/// The icon of the DM group has been changed.
		/// </summary>
		ChannelIconChange = 5,

		/// <summary>
		/// A message has been pinned.
		/// </summary>
		ChannelPinnedMessage = 6,

		/// <summary>
		/// A member joined the guild.
		/// </summary>
		GuildMemberJoined = 7,

		/// <summary>
		/// A member boosted the server.
		/// </summary>
		UserBoosted = 8,

		/// <summary>
		/// A member boosted the server to T1.
		/// </summary>
		UserBoostedT1 = 9,

		/// <summary>
		/// A member boosted the server to T2.
		/// </summary>
		UserBoostedT2 = 10,

		/// <summary>
		/// A member boosted the server to T3.
		/// </summary>
		UserBoostedT3 = 11,

		/// <summary>
		/// A channel was followed into this one, and so any published messages from said channel will be reposted here.
		/// </summary>
		ChannelFollowAdded = 12,

		// 13 lol

		/// <summary>
		/// A notification mentioning that this guild has been disqualified from discovery.
		/// </summary>
		GuildDiscoveryDisqualified = 14,

		/// <summary>
		/// A notification mentioning that this guild now qualifies for discovery.
		/// </summary>
		GuildDiscoveryRequalified = 15,

		/// <summary>
		/// This message is a reply to someone else's message.
		/// </summary>
		Reply = 19,

		/// <summary>
		/// This message is an application command e.g. /pootis as a custom command.
		/// </summary>
		ApplicationCommand = 20,

		/// <summary>
		/// This message is the starting message for a thread, which the creator of a thread is prompted for when creating the thread. It doubles
		/// as the thread's effective "topic" in a manner akin to a channel topic.
		/// </summary>
		ThreadStarterMessage = 21,

		/// <summary>
		/// The specifics of this are unknown.
		/// </summary>
		GuildInviteReminder = 22

	}
}

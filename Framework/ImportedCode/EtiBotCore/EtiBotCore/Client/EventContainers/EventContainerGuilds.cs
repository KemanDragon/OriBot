#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.Events.Intents.Guilds;
using EtiBotCore.Utility.Extension;
using EtiLogger.Logging;
using SignalCore;

namespace EtiBotCore.Client.EventContainers {

	/// <summary>
	/// Contains all event handlers for the <see cref="GatewayIntent.GUILDS"/> intent.
	/// </summary>
	public class EventContainerGuilds {

		internal EventContainerGuilds() { }

		/// <summary>
		/// This event fires when a guild object is created. This is the best event to use for initializing the data of a server.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server</c>
		/// </remarks>
		public Signal<Guild> OnGuildCreated { get; set; } = new Signal<Guild>();

		/// <summary>
		/// This event fires when a property of a guild is changed.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>serverBefore, serverAfter</c>
		/// </remarks>
		public Signal<Guild, Guild> OnGuildUpdated { get; set; } = new Signal<Guild, Guild>();

		/// <summary>
		/// This event fires when a server is rendered unavailable, for instance, due to leaving, an outage, or being kicked/banned.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, isUnavailable</c>
		/// </remarks>
		public Signal<Guild, bool> OnGuildDeleted { get; set; } = new Signal<Guild, bool>();

		/// <summary>
		/// This event fires when a new role is created in a server.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, newRole</c>
		/// </remarks>
		public Signal<Guild, Role> OnRoleCreated { get; set; } = new Signal<Guild, Role>();

		/// <summary>
		/// This event fires when a role is changed.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, roleBefore, roleAfter</c>
		/// </remarks>
		public Signal<Guild, Role, Role> OnRoleUpdated { get; set; } = new Signal<Guild, Role, Role>();

		/// <summary>
		/// This event fires when a role is deleted. The given <see cref="Role"/> object may be <see langword="null"/>, so its <see cref="Snowflake"/> is provided.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, roleIfExists, roleID</c>
		/// </remarks>
		public Signal<Guild, Role, Snowflake> OnRoleDeleted { get; set; } = new Signal<Guild, Role, Snowflake>();

		/// <summary>
		/// This event fires when a channel is created.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>newChannel</c>
		/// </remarks>
		public Signal<ChannelBase> OnChannelCreated { get; set; } = new Signal<ChannelBase>();

		/// <summary>
		/// This event fires when a channel is changed.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>channelBefore, channelAfter</c>
		/// </remarks>
		public Signal<ChannelBase, ChannelBase> OnChannelUpdated { get; set; } = new Signal<ChannelBase, ChannelBase>();

		/// <summary>
		/// This event fires when a channel is deleted. The given <see cref="ChannelBase"/> may be <see langword="null"/>, so its <see cref="Snowflake"/> is provided.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>channelIfExists, channelId</c>
		/// </remarks>
		public Signal<ChannelBase, Snowflake> OnChannelDeleted { get; set; } = new Signal<ChannelBase, Snowflake>();

		/// <summary>
		/// This event fires when a channel's pin list is changed in any way. Discord does not include the message, nor does it include whether a message was pinned or unpinned. Isn't that grand?
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, inChannel, whenOccurred</c>
		/// </remarks>
		public Signal<Guild, TextChannel, DateTimeOffset?> OnPinsUpdated { get; set; } = new Signal<Guild, TextChannel, DateTimeOffset?>();

		/// <summary>
		/// This event fires when a new thread is created. It contains a full thread object.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>newThreadInstance</c>
		/// </remarks>
		public Signal<Thread> OnThreadCreated { get; set; } = new Signal<Thread>();

		/// <summary>
		/// This event fires when an existing thread is updated. Changes to the last_message_id field will not fire this event.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>threadBefore, threadNow</c>
		/// </remarks>
		public Signal<Thread, Thread> OnThreadUpdated { get; set; } = new Signal<Thread, Thread>();

		/// <summary>
		/// This event fires when a thread is completely deleted (not archived).<para/>
		/// The <c>threadIfExists</c> (first) parameter can be used, but may be null, from which the other four params must be used.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>threadIfExists, threadID, serverID, parentChannelID, channelType</c>
		/// </remarks>
		public Signal<Thread, Snowflake, Snowflake, Snowflake, ChannelType> OnThreadDeleted { get; set; } = new Signal<Thread, Snowflake, Snowflake, Snowflake, ChannelType>();

		/// <summary>
		/// This event fires when the list of threads in the server needs to be synchronized. 
		/// If the second parameter (<see cref="GuildChannelBase"/>[]) is null, then this contains all threads in the entire server.
		/// Otherwise, it contains the IDs of the parent channels, from which all child threads will update.<para/>
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, parentChannelsUpdating, threads</c>
		/// </remarks>
		public Signal<Guild, GuildChannelBase[], Thread[]> OnThreadListSync { get; set; } = new Signal<Guild, GuildChannelBase[], Thread[]>();

		/// <summary>
		/// This event fires when the current user updates in a thread. It is unlikely to be used by bots, according to Discord, in favor of <see cref="OnThreadMembersUpdated"/>.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>thisThreadMember</c>
		/// </remarks>
		public Signal<User> OnSingleThreadMemberUpdated { get; set; } = new Signal<User>();

		/// <summary>
		/// This event fires when anyone is added to or removed from a thread.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>threadID, serverID, addedMembers, removedMemberIDs</c><para/>
		/// <strong>Limits:</strong> If <see cref="GatewayIntent.GUILD_MEMBERS"/> is not enabled, this will strictly only fire for the bot itself and nobody else.
		/// </remarks>
		public Signal<Guild, Thread, Member[], Snowflake[]> OnThreadMembersUpdated { get; set; } = new Signal<Guild, Thread, Member[], Snowflake[]>();

		// TODO: Stage channel stuff
	}
}

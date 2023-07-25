#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Payloads.Data;
using SignalCore;

namespace EtiBotCore.Client.EventContainers {

	/// <summary>
	/// Contains all event handlers for the <see cref="GatewayIntent.GUILD_MEMBERS"/> intent.
	/// </summary>
	public class EventContainerMembers {

		internal EventContainerMembers() { }

		/// <summary>
		/// This event fires when a member is added to a guild.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, member</c>
		/// </remarks>
		public Signal<Guild, Member> OnGuildMemberAdded { get; set; } = new Signal<Guild, Member>();

		/// <summary>
		/// This event fires when a member changes (for instance, they change their nickname or the roles they have changes).<para/>
		/// Note: In some cases, this may be desynchronized. It is strongly advised to test <see cref="Member.IsShallow"/> to determine if all of the data is present.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, memberBefore, memberAfter</c>
		/// </remarks>
		public Signal<Guild, Member, Member> OnGuildMemberUpdated { get; set; } = new Signal<Guild, Member, Member>();

		/// <summary>
		/// This event fires when a member is removed from the guild for any reason.<para/>
		/// Note: In some cases, this may be desynchronized. It is strongly advised to test <see cref="Member.IsShallow"/> to determine if all of the data is present.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>server, member</c>
		/// </remarks>
		public Signal<Guild, Member> OnGuildMemberRemoved { get; set; } = new Signal<Guild, Member>();

	}
}

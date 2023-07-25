using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {

	/// <summary>
	/// Run when a role is deleted in a guild.
	/// </summary>
	internal class GuildRoleDeleteEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the server this event occurred in.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong GuildID { get; set; }

		/// <summary>
		/// The ID of the role that was deleted.
		/// </summary>
		[JsonProperty("role_id")]
		public ulong RoleID { get; set; }

		public async Task Execute(DiscordClient fromClient) {
			var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID);
			var role = guild.Roles[RoleID];
			if (role != null) {
				role.Deleted = true;
			}
			guild.Roles.RemoveInternally(RoleID);
			//await fromClient.Events.GuildEvents.InvokeOnRoleDeleted(guild, role, RoleID);
			await fromClient.Events.GuildEvents.OnRoleDeleted.Invoke(guild, role, RoleID);
		}
	}
}

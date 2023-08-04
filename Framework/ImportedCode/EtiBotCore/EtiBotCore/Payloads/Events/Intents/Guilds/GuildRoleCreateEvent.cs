using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {

	/// <summary>
	/// Run when a role is added to a guild.
	/// </summary>
	internal class GuildRoleCreateEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the server this event occurred in.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong GuildID { get; set; }

		/// <summary>
		/// The role that was created.
		/// </summary>
		[JsonProperty("role"), JsonRequired]
		public Role Role { get; set; } = new Role();

		public async Task Execute(DiscordClient fromClient) {
			var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID);
			DiscordObjects.Guilds.Role role = DiscordObjects.Guilds.Role.GetOrCreate(Role, guild);
			role.Deleted = false;
			if (!guild.Roles.Contains(role.ID)) {
				guild.Roles.AddInternally(role);
			}
			//await fromClient.Events.GuildEvents.InvokeOnRoleCreated(guild, role);
			await fromClient .Events.GuildEvents.OnRoleCreated.Invoke(guild, role);
		}
	}
}

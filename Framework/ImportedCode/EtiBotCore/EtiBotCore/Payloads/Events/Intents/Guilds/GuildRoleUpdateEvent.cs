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
	/// Run when a role is changed in a guild.
	/// </summary>
	internal class GuildRoleUpdateEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the server this event occurred in.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong GuildID { get; set; }

		/// <summary>
		/// The the role that was changed (which has its new properties)
		/// </summary>
		[JsonProperty("role"), JsonRequired]
		public Role Role { get; set; } = new Role();

		public async Task Execute(DiscordClient fromClient) {
			int tries = 0;
			while (DiscordObjects.Guilds.Role.RoleRegistry.TryGetValue(Role.ID, out var _) == false && tries < 10) {
				tries++;
				await Task.Delay(500);
			}
			if (tries == 10) {
				DiscordClient.Log.WriteCritical("Role update received before the actual role was received, and the role still wasn't received after waiting a while. I'm dropping this event.", EtiLogger.Logging.LogLevel.Trace);
				return;
			}
			var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID);
			var role = DiscordObjects.Guilds.Role.GetOrCreate(Role, guild);
			var oldRole = (DiscordObjects.Guilds.Role)role.MemberwiseClone();
			role.Deleted = false;
			//await fromClient.Events.GuildEvents.InvokeOnRoleUpdated(guild, oldRole, role);
			await fromClient.Events.GuildEvents.OnRoleUpdated.Invoke(guild, oldRole, role);
		}
	}
}

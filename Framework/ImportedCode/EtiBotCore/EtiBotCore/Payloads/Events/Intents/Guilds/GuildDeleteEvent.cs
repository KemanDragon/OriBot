using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Payloads.PayloadObjects;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {

	/// <summary>
	/// Runs when a guild has been deleted, a guild becomes available due to an outage, or when this bot leaves said guild. Identical to an unavailable guild.
	/// </summary>
	internal class GuildDeleteEvent : UnavailableGuild, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			var guild = DiscordObjects.Universal.Guild.GetOrCreateUnavailableFromPayload(this);
			await guild.UpdateFromObject(this, false);
			//await fromClient.Events.GuildEvents.InvokeOnGuildDeleted(guild, Unavailable);
			await fromClient.Events.GuildEvents.OnGuildDeleted.Invoke(guild, Unavailable);
		}
	}
}

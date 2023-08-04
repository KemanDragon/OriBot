using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Payloads.PayloadObjects;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {
	
	/// <summary>
	/// Run when the settings of a guild are changed. It is identical to a <see cref="Guild"/> object.
	/// </summary>
	internal class GuildUpdateEvent : Guild, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			var guild = await DiscordObjects.Universal.Guild.GetOrCreateFromPayload(this);
			var oldGuild = (DiscordObjects.Universal.Guild)guild.MemberwiseClone();
			await guild.UpdateFromObject(this, false);
			//await fromClient.Events.GuildEvents.InvokeOnGuildUpdated(oldGuild, guild);
			await fromClient.Events.GuildEvents.OnGuildUpdated.Invoke(oldGuild, guild);
		}
	}
}

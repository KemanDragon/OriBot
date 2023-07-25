using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {

	/// <summary>
	/// Run when a server has been created. This is identical to a <see cref="Guild"/> object.
	/// </summary>
	internal class GuildCreateEvent : Guild, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			var guild = await DiscordObjects.Universal.Guild.GetOrCreateFromPayload(this);
			await guild.UpdateFromObject(this, false);
			//await fromClient.Events.GuildEvents.InvokeOnGuildCreated(guild);
			await fromClient .Events.GuildEvents.OnGuildCreated.Invoke(guild);
		}
	}
}

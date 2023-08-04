using EtiBotCore.Client;
using EtiBotCore.Payloads.PayloadObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {
	internal class ThreadUpdateEvent : Channel, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			var threadObj = await DiscordObjects.Base.GuildChannelBase.GetOrCreateAsync<DiscordObjects.Guilds.Thread>(this);
			var oldThread = threadObj.MemberwiseClone<DiscordObjects.Guilds.Thread>();
			await threadObj.UpdateFromObject(this, true);
			await fromClient.Events.GuildEvents.OnThreadUpdated.Invoke(oldThread, threadObj);
		}
	}
}

using EtiBotCore.Client;
using EtiBotCore.Payloads.PayloadObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {
	internal class ThreadCreateEvent : Channel, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			var threadObj = await DiscordObjects.Base.GuildChannelBase.GetOrCreateAsync<DiscordObjects.Guilds.Thread>(this);
			threadObj.Server.RegisterChannel(threadObj);
			await threadObj.UpdateFromObject(this, false);
			await fromClient.Events.GuildEvents.OnThreadCreated.Invoke(threadObj);
		}
	}
}

using EtiBotCore.Client;
using EtiBotCore.Payloads.PayloadObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {
	internal class ThreadMemberUpdateEvent : ThreadMember, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			await fromClient.Events.GuildEvents.OnSingleThreadMemberUpdated.Invoke(DiscordObjects.Universal.User.BotUser);
		}
	}
}

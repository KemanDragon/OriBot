using EtiBotCore.Client;
using EtiBotCore.Payloads.PayloadObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {
	class ThreadListSyncEvent : ThreadSyncData, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			var server = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(ServerID);
			IEnumerable<DiscordObjects.Base.GuildChannelBase> channels;
			if (UpdatedParents == null) {
				channels = server.TextChannels;
			} else {
				channels = server.TextChannels.Where(channel => UpdatedParents!.Contains(channel.ID));
			}
			DiscordObjects.Guilds.Thread[] threads = new DiscordObjects.Guilds.Thread[Threads.Length];
			for (int i = 0; i < threads.Length; i++) {
				threads[i] = await DiscordObjects.Base.GuildChannelBase.GetOrCreateAsync<DiscordObjects.Guilds.Thread>(Threads[i], server);
				await threads[i].UpdateFromObject(this, true);
			}

			await fromClient.Events.GuildEvents.OnThreadListSync.Invoke(server, channels.ToArray(), threads);
		}
	}
}

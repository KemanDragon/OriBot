using EtiBotCore.Client;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {

	/// <summary>
	/// Run when a channel is updated or changed in a guild. This is identical to a <see cref="Channel"/>.
	/// </summary>
	internal class ChannelUpdateEvent : Channel, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			int tries = 0;
			while (GuildChannelBase.InstantiatedChannelsByID.TryGetValue(ID, out var _) == false && tries < 10) {
				tries++;
				await Task.Delay(500);
			}
			if (tries == 10) {
				DiscordClient.Log.WriteCritical("Channel update received before the actual channel was received, and the channel still wasn't received after waiting a while. I'm dropping this event.", EtiLogger.Logging.LogLevel.Trace);
				return;
			}
			GuildChannelBase channel = await GuildChannelBase.GetOrCreateAsync<GuildChannelBase>(this);
			GuildChannelBase oldChannel = (GuildChannelBase)channel.MemberwiseClone();
			await channel.UpdateFromObject(this, true);
			//await fromClient.Events.GuildEvents.InvokeOnChannelUpdated(oldChannel, channel);
			await fromClient.Events.GuildEvents.OnChannelUpdated.Invoke(oldChannel, channel);
		}
	}
}

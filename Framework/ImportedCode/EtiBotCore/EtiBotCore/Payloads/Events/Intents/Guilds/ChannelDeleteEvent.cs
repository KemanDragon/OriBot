using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {

	/// <summary>
	/// Run when a channel is deleted in a guild. This is identical to a <see cref="Channel"/>.
	/// </summary>
	internal class ChannelDeleteEvent : Channel, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			ChannelBase? channel = null;
			if (GuildID != null) {
				var server = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID.Value);
				channel = server.GetChannel(ID);
			} else {
				if (DMChannel.DMChannelCache.TryGetValue(ID, out DMChannel? dmChannel)) {
					channel = dmChannel;
				}
			}
			if (channel != null) {
				channel.Deleted = true;
				if (EagerPinTracker.HasTracker(channel)) {
					EagerPinTracker.GetTrackerFor(channel).TellChannelWasDeleted();
				}
			}
			//await fromClient.Events.GuildEvents.InvokeOnChannelDeleted(channel, ID);
			await fromClient.Events.GuildEvents.OnChannelDeleted.Invoke(channel, ID);
		}
	}
}

using EtiBotCore.Client;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {

	/// <summary>
	/// Run when a channel is created in a guild. This is identical to a <see cref="Channel"/>.
	/// </summary>
	internal class ChannelCreateEvent : Channel, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			ChannelBase? channel = null;

			if (Type == ChannelType.Text || Type == ChannelType.News || Type == ChannelType.Store || Type == ChannelType.Category || Type == ChannelType.Voice) {
				channel = await GuildChannelBase.GetOrCreateAsync<GuildChannelBase>(this);
			} else if (Type == ChannelType.DM || Type == ChannelType.GroupDM) {
				channel = await DMChannel.GetOrCreateAsync(this);

			}
			if (channel is GuildChannelBase guildChannel) {
				var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID!.Value);
				guild.RegisterChannel(guildChannel);
			}
			if (channel != null) {
				await channel.UpdateFromObject(this, true);
				//await fromClient.Events.GuildEvents.InvokeOnChannelCreated(channel);
				await fromClient.Events.GuildEvents.OnChannelCreated.Invoke(channel);
			}
		}
	}
}

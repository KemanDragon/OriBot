using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;

namespace EtiBotCore.Payloads.Events.Intents.GuildOrDirectMessages {

	/// <summary>
	/// Fired when a message is updated (edited). Identical to a <see cref="Message"/>, but only a fraction of the data is contained. ID and channel ID will always exist.
	/// </summary>
	internal class MessageUpdateEvent : Message, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			bool? pinState = null;
			if (!DiscordObjects.Guilds.ChannelData.Message.InstantiatedMessages.TryGetValue(ID, out var existingMsg)) {
				int tries = 0;
				do {
					tries++;
					await Task.Delay(500);
				} while (DiscordObjects.Guilds.ChannelData.Message.InstantiatedMessages.TryGetValue(ID, out existingMsg) == false && tries < 5);
				if (tries == 5) {
					var ch = DiscordObjects.Base.GuildChannelBase.GetFromCache<TextChannel>(ChannelID);
					if (ch != null) {
						await ch.GetMessageAsync(ID);
					}
					return;
				}
			}
			if (existingMsg!.Pinned != Pinned) {
				pinState = Pinned;
			}

			var msg = await DiscordObjects.Guilds.ChannelData.Message.GetOrCreateAsync(this);
			var oldMsg = (DiscordObjects.Guilds.ChannelData.Message)msg.MemberwiseClone();
			await msg.UpdateFromObject(this, false);
			await fromClient.Events.MessageEvents.OnMessageEdited.Invoke(oldMsg, msg, pinState);
		}
	}
}

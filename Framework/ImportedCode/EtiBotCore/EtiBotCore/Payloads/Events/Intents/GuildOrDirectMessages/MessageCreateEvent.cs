using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Payloads.PayloadObjects;

namespace EtiBotCore.Payloads.Events.Intents.GuildOrDirectMessages {

	/// <summary>
	/// Fires when a message is sent. It is identical to a <see cref="Message"/> object.
	/// </summary>
	internal class MessageCreateEvent : Message, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			var msg = await DiscordObjects.Guilds.ChannelData.Message.GetOrCreateAsync(this);
			await fromClient.Events.MessageEvents.OnMessageCreated.Invoke(msg, msg.Pinned);
		}
	}
}

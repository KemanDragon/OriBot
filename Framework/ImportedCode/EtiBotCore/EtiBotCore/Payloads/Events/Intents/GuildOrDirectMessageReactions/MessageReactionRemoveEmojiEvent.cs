using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.GuildOrDirectMessageReactions {

	/// <summary>
	/// Fires when a specific emoji is removed from a message's reactions (all instances of that emoji at once)
	/// </summary>
	internal class MessageReactionRemoveEmojiEvent : MessageReactionRemoveAllEvent, IEvent {

		/// <summary>
		/// The Emoji that was removed. This is a lightweight emoji containing only its name.
		/// </summary>
		[JsonProperty("emoji"), JsonRequired]
		public Emoji Emoji { get; set; } = new Emoji();

		public override async Task Execute(DiscordClient fromClient) {
			DiscordObjects.Guilds.ChannelData.Message message;
			ChannelBase channel;
			if (GuildID != null) {
				var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID.Value);
				channel = guild.GetChannel(ChannelID)!;
				message = await ((TextChannel)channel).GetMessageAsync(MessageID);
			} else {
				channel = await DMChannel.GetOrCreateAsync(ChannelID);
				message = await ((DMChannel)channel).GetMessageAsync(MessageID);
			}

			DiscordObjects.Universal.Emoji emoji = Emoji.Creator == null ? DiscordObjects.Universal.Emoji.GetOrCreate(Emoji.Name!) : DiscordObjects.Universal.CustomEmoji.GetOrCreate(Emoji);
			message.Reactions.RemoveAllReactionsOf(emoji);

			await fromClient.Events.ReactionEvents.OnAllReactionsOfEmojiRemoved.Invoke(message, emoji);
		}

	}
}

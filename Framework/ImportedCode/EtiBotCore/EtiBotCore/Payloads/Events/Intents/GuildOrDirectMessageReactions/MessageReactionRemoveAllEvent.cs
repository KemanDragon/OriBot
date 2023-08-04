using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.GuildOrDirectMessageReactions {

	/// <summary>
	/// Fires when all reactions are removed from a message.
	/// </summary>
	internal class MessageReactionRemoveAllEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the channel that the message exists in.
		/// </summary>
		[JsonProperty("channel_id")]
		public ulong ChannelID { get; set; }

		/// <summary>
		/// The ID of the message that was reacted to.
		/// </summary>
		[JsonProperty("message_id")]
		public ulong MessageID { get; set; }

		/// <summary>
		/// The ID of the guild this reaction was added in, or <see langword="null"/> if it was done in a DM.
		/// </summary>
		[JsonProperty("guild_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? GuildID { get; set; }

		public virtual async Task Execute(DiscordClient fromClient) {
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
			message.Reactions.RemoveAll();

			await fromClient.Events.ReactionEvents.OnAllReactionsRemoved.Invoke(message);
		}
	}
}

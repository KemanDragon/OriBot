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
	/// Fires when a reaction is removed from a message.
	/// </summary>
	internal class MessageReactionRemoveEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the user that added this reaction.
		/// </summary>
		[JsonProperty("user_id")]
		public ulong UserID { get; set; }

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

		/// <summary>
		/// The emoji that was added. It is a minimal representation with only its name.
		/// </summary>
		[JsonProperty("emoji"), JsonRequired]
		public Emoji Emoji { get; set; } = new Emoji();

		public virtual async Task Execute(DiscordClient fromClient) {
			DiscordObjects.Guilds.ChannelData.Message message;
			ChannelBase channel;
			var reactor = await DiscordObjects.Universal.User.GetOrDownloadUserAsync(UserID);

			if (GuildID != null) {
				var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID.Value);
				channel = guild.GetChannel(ChannelID)!;
				message = await ((TextChannel)channel).GetMessageAsync(MessageID);
			} else {
				channel = await DMChannel.GetOrCreateAsync(ChannelID);
				message = await ((DMChannel)channel).GetMessageAsync(MessageID);
			}
			DiscordObjects.Universal.Emoji emoji = Emoji.ID == null ? DiscordObjects.Universal.Emoji.GetOrCreate(Emoji.Name!) : DiscordObjects.Universal.CustomEmoji.GetOrCreate(Emoji);
			message.Reactions.RemoveReactionFrom(reactor!, emoji);
			if (message.Reactions.EagerTracking && message.Reactions.ShouldEagerDownload(emoji)) {
				await message.Reactions.DownloadReactions(emoji, 100, true, true);
			}

			await fromClient.Events.ReactionEvents.OnReactionRemoved.Invoke(message, emoji, reactor!);
		}
	}
}

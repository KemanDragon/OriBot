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
using EtiLogger.Logging;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.GuildOrDirectMessageReactions {

	/// <summary>
	/// Fired when a reaction is added to a message.
	/// </summary>
	internal class MessageReactionAddEvent : MessageReactionRemoveEvent, IEvent {
		
		/// <summary>
		/// The member who added the reaction, or <see langword="null"/> if this reaction was added in a DM channel.
		/// </summary>
		[JsonProperty("member", NullValueHandling = NullValueHandling.Ignore)]
		public PayloadObjects.Member? Member { get; set; }

		public override async Task Execute(DiscordClient fromClient) {
			DiscordObjects.Guilds.ChannelData.Message message;
			ChannelBase channel;
			var reactor = await DiscordObjects.Universal.User.GetOrDownloadUserAsync(UserID);

			if (GuildID != null) {
				var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID.Value);
				channel = guild.GetChannel(ChannelID)!;
				message = await ((TextChannel)channel).GetMessageAsync(MessageID);
			/*} else if (GuildChannelBase.InstantiatedChannelsByID.TryGetValue(ChannelID, out GuildChannelBase? gChannel)) {
				channel = gChannel;
				message = ((TextChannel)channel).GetMessage(MessageID).RunSync();*/
			} else {
				channel = await DMChannel.GetOrCreateAsync(ChannelID);
				message = await ((DMChannel)channel).GetMessageAsync(MessageID);
			}

			DiscordObjects.Universal.Emoji emoji = Emoji.ID == null ? DiscordObjects.Universal.Emoji.GetOrCreate(Emoji.Name!) : DiscordObjects.Universal.CustomEmoji.GetOrCreate(Emoji);
			message.Reactions.AddReactionFrom(reactor!, emoji);
			if (message.Reactions.EagerTracking && message.Reactions.ShouldEagerDownload(emoji)) {
				await message.Reactions.DownloadReactions(emoji, 100, true, true);
			}
			
			await fromClient.Events.ReactionEvents.OnReactionAdded.Invoke(message, emoji, reactor!);
		}

	}
}

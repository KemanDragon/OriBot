using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.GuildOrDirectMessages {

	/// <summary>
	/// Fired when messages are deleted in bulk.
	/// </summary>
	internal class MessageDeleteBulkEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The IDs of the messages.
		/// </summary>
		[JsonProperty("ids"), JsonRequired]
		public ulong[] IDs { get; set; } = new ulong[0];

		/// <summary>
		/// The ID of the channel this message is in.
		/// </summary>
		[JsonProperty("channel_id")]
		public ulong ChannelID { get; set; }

		/// <summary>
		/// The ID of the server that the channel is in, or <see langword="null"/> if this occurred in a DM.
		/// </summary>
		[JsonProperty("guild_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? GuildID { get; set; }

		public async Task Execute(DiscordClient fromClient) {
			fromClient.Events.MessageEvents.BulkMessages.AddRange(IDs.Cast<Snowflake>()); // Do this IMMEDIATELY
			// The block below might yield for a guild download, and while that will stall any incoming events (as of writing)
			// it may not guarantee that the invocation at the bottom is called before the other events are received.
			// This doesn't guarantee it either, but it makes it more likely.

			ChannelBase channel;
			if (GuildID != null) {
				var server = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID.Value);
				TextChannel cn = (TextChannel)server.GetChannel(ChannelID)!;
				foreach (var message in cn.Messages.Values) {
					if (IDs.Contains(message.ID)) {
						message.Deleted = true;
					}
				}
				channel = cn;
			} else {
				DMChannel cn = await DMChannel.GetOrCreateAsync(ChannelID);
				foreach (var message in cn.Messages.Values) {
					if (IDs.Contains(message.ID)) {
						message.Deleted = true;
					}
				}
				channel = cn;
			}
			await fromClient.Events.MessageEvents.OnMessagesBulkDeleted.Invoke(IDs.Cast<Snowflake>().ToArray(), channel);
		}
	}
}

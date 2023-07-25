using EtiBotCore.Client;
using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {

	/// <summary>
	/// Run when a message is pinned or unpinned in a channel. This does not fire if a pinned message is deleted.
	/// </summary>
	internal class ChannelPinsUpdateEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the server that the message was pinned in, or <see langword="null"/> if this is is a DM channel.
		/// </summary>
		[JsonProperty("guild_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? GuildID { get; set; }

		/// <summary>
		/// The ID of the channel that the message was pinned in.
		/// </summary>
		[JsonProperty("channel_id")]
		public ulong ChannelID { get; set; }

		/// <summary>
		/// The timestamp of when the latest message was pinned in this channel.
		/// </summary>
		[JsonProperty("last_pin_timestamp", NullValueHandling = NullValueHandling.Ignore), JsonConverter(typeof(TimestampConverter))]
		public ISO8601? LastPinTimestamp { get; set; }

		public async Task Execute(DiscordClient fromClient) {
			if (GuildID != null) {
				var server = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID.Value);
				var channel = server.GetChannel(ChannelID);
				//await fromClient.Events.GuildEvents.InvokeOnPinsUpdated(server, channel as TextChannel, LastPinTimestamp?.DateTime);
				await fromClient.Events.GuildEvents.OnPinsUpdated.Invoke(server, channel as TextChannel, LastPinTimestamp?.DateTime);
			} else {
				await fromClient.Events.MessageEvents.OnDirectMessagePinStateChanged.Invoke(await DMChannel.GetOrCreateAsync(ChannelID));
			}
			
		}
	}
}

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
	/// Fires when a message is deleted.
	/// </summary>
	internal class MessageDeleteEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the message.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// The ID of the channel this message is in.
		/// </summary>
		[JsonProperty("channel_id")]
		public ulong ChannelID { get; set; }

		/// <summary>
		/// The ID of the server that the channel is in, or <see langword="null"/> if this is in a DM.
		/// </summary>
		[JsonProperty("guild_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? GuildID { get; set; }

		public async Task Execute(DiscordClient fromClient) {
			ChannelBase channel;
			if (GuildID != null) {
				var server = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID.Value);
				TextChannel cn = (TextChannel)server.GetChannel(ChannelID)!;
				// var msg = cn.Messages.Find(msg => msg.ID == ID);
				// if (msg != null) msg.Deleted = true;
				if (cn.Messages.TryGetValue(ID, out var msg)) msg.Deleted = true;
				channel = cn;
			} else {
				DMChannel cn = await DMChannel.GetOrCreateAsync(ChannelID);
				//var msg = cn.Messages.Find(msg => msg.ID == ID);
				//if (msg != null) msg.Deleted = true;
				if (cn.Messages.TryGetValue(ID, out var msg)) msg.Deleted = true;
				channel = cn;
			}
			await fromClient.Events.MessageEvents.OnMessageDeleted.Invoke(ID, channel);
		}
	}
}

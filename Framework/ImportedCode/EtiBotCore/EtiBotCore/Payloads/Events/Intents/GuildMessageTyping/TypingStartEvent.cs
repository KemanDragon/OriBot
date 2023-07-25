using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.GuildMessageTyping {

	/// <summary>
	/// Fires when typing starts in a channel.
	/// </summary>
	internal class TypingStartEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The channel that they started typing in.
		/// </summary>
		[JsonProperty("channel_id")]
		public ulong ChannelID { get; set; }

		/// <summary>
		/// The server they started typing in, or <see langword="null"/> if it's a DM.
		/// </summary>
		[JsonProperty("guild_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? GuildID { get; set; }

		/// <summary>
		/// The ID of the user that started typing.
		/// </summary>
		[JsonProperty("user_id")]
		public ulong UserID { get; set; }

		/// <summary>
		/// The unix time (in seconds) of when they started typing.
		/// </summary>
		[JsonProperty("timestamp")]
		public int Timestamp { get; set; }

		/// <summary>
		/// The member who started typing if this occurred in a server, or <see langword="null"/> if this occurred in a DM.
		/// </summary>
		[JsonProperty("member", NullValueHandling = NullValueHandling.Ignore)]
		public Member? Member { get; set; }

		public async Task Execute(DiscordClient fromClient) {
			ChannelBase channel;
			var user = await DiscordObjects.Universal.User.GetOrDownloadUserAsync(UserID);
			if (GuildID != null) {
				var server = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID.Value);
				channel = server.GetChannel(ChannelID)!;
			} else {
				channel = await DMChannel.GetOrCreateAsync(ChannelID);
			}
			await fromClient.Events.TypingEvents.OnTypingStarted.Invoke(user!, channel, DateTimeOffset.FromUnixTimeSeconds(Timestamp));
		}
	}
}

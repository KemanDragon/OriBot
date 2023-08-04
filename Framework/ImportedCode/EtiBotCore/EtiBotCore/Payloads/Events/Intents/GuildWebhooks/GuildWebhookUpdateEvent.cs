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

namespace EtiBotCore.Payloads.Events.Intents.GuildWebhooks {

	/// <summary>
	/// Fired when a webhook is created, removed, or changed in a guild.
	/// </summary>
	internal class GuildWebhookUpdateEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the guild that the webhook changed in.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong GuildID { get; set; }

		/// <summary>
		/// The channel that the webhook is associated with.
		/// </summary>
		[JsonProperty("channel_id")]
		public ulong ChannelID { get; set; }

		public async Task Execute(DiscordClient fromClient) {
			var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID);
			GuildChannelBase channel = guild.GetChannel(ChannelID)!;
			await fromClient.Events.WebhookEvents.OnWebhooksUpdated.Invoke(guild, channel);
		}
	}
}

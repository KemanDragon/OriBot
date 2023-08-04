using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.GuildIntegrations {

	/// <summary>
	/// Fires when a guild integration is added, removed, or changed.
	/// </summary>
	internal class GuildIntegrationsUpdateEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the server that this change occurred in.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong GuildID { get; set; }

		public async Task Execute(DiscordClient fromClient) {
			var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID);
			await fromClient.Events.IntegrationEvents.OnIntegrationsUpdated.Invoke(guild);
		}
	}
}

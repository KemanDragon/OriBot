using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Passthrough {

	/// <summary>
	/// Run when the voice server location of the guild changes.
	/// </summary>
	internal class VoiceServerUpdateEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The voice connection token.
		/// </summary>
		[JsonProperty("token"), JsonRequired]
		public string Token { get; set; } = string.Empty;

		/// <summary>
		/// The ID of the guild this applies to.
		/// </summary>
		[JsonProperty("guild_id"), JsonRequired]
		public ulong GuildID { get; set; }

		/// <summary>
		/// The endpoint of the new host. 
		/// A <see langword="null"/> endpoint means that the voice server allocated has gone away and is trying to be reallocated. You should attempt to disconnect from the currently connected voice server, and not attempt to reconnect until a new voice server is allocated.
		/// </summary>
		[JsonProperty("endpoint"), JsonRequired]
		public string? Endpoint { get; set; }

		public async Task Execute(DiscordClient fromClient) {
			//var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID, true);
			//guild._VoiceRegion = Endpoint;
			await fromClient.Events.PassthroughEvents.OnVoiceServerUpdated.Invoke(GuildID, Token, Endpoint);
		}

	}
}

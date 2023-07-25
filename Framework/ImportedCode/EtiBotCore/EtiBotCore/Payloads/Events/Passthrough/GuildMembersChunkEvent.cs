using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Payloads.Events.Intents.GuildPresences;
using EtiBotCore.Payloads.PayloadObjects;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Passthrough {

	/// <summary>
	/// A response to the guild request members event.
	/// </summary>
	internal class GuildMembersChunkEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the guild these members are a part of.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong GuildID { get; set; }

		/// <summary>
		/// The members being sent with this chunk. At most, this will contain 1000 members.
		/// </summary>
		[JsonProperty("members")]
		public Member[] Members { get; set; } = new Member[0];

		/// <summary>
		/// The index of this chunk.
		/// </summary>
		[JsonProperty("chunk_index")]
		public int ChunkIndex { get; set; }

		/// <summary>
		/// The total amount of chunks that will be returned by the member request.
		/// </summary>
		[JsonProperty("chunk_count")]
		public int ChunkCount { get; set; }

		/// <summary>
		/// Any IDs that were invalid in the request that warranted this response will be in this array. <see langword="null"/> if all IDs were valid.
		/// </summary>
		[JsonProperty("not_found", NullValueHandling = NullValueHandling.Ignore)]
		public ulong[]? NotFound { get; set; }

		/// <summary>
		/// If passing <see langword="true"/> into the member request to get presences, they will be listed here. It is <see langword="null"/> otherwise.
		/// </summary>
		[JsonProperty("presences", NullValueHandling = NullValueHandling.Ignore)]
		public PresenceUpdateEvent[]? Presences { get; set; }

		/// <summary>
		/// A unique string that you defined, specifically the one sent in the member request that warranted this response.
		/// </summary>
		[JsonProperty("nonce", NullValueHandling = NullValueHandling.Ignore)]
		public string? Nonce { get; set; }

		public Task Execute(DiscordClient fromClient) {
			fromClient.GuildChunkReceived(this);
			return Task.CompletedTask;
		}
	}
}

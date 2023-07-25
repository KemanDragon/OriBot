using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Commands {
	class RequestGuildMembersCommand : PayloadDataObject {

		/// <summary>
		/// The ID of the guild to get members from.
		/// </summary>
		[JsonProperty("guild_id")]
		public Snowflake GuildID { get; set; }

		/// <summary>
		/// A string the username starts with, or an empty string to return all members.
		/// </summary>
		[JsonProperty("query")]
		public string Query { get; set; } = string.Empty;

		/// <summary>
		/// A limit for the amount of members to send when using <see cref="Query"/>. Max 100. When not using a query, this can be 0 to return all members.
		/// </summary>
		[JsonProperty("limit")]
		public int Limit { get; set; } = 0;

		/// <summary>
		/// Used to also get the presences of users.
		/// </summary>
		[JsonProperty("presences")]
		public bool Presences { get; set; } = false;

		/// <summary>
		/// The list of users to get.
		/// </summary>
		[JsonProperty("user_ids")]
		public Snowflake[]? Users { get; set; }

		/// <summary>
		/// A unique identifier you define right here that is given back in the chunk response.
		/// </summary>
		[JsonProperty("nonce")]
		public string? Nonce { get; set; }

	}
}

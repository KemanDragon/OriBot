using EtiBotCore.Client;
using EtiBotCore.Payloads.PayloadObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Passthrough {

	/// <summary>
	/// An event that fires when Discord is ready to receive payloads.
	/// </summary>
	internal class ReadyEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The gateway version being implemented.
		/// </summary>
		[JsonProperty("v")]
		public int Version { get; set; }

		/// <summary>
		/// Information about the bot user.
		/// </summary>
		[JsonProperty("user"), JsonRequired]
		public User User { get; set; } = new User();

		[JsonProperty("private_channels")]
		[SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "The private channel array has not been implemented by Discord and is completely useless, but must be included as per the specifications of the API.")]
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
		private object[] private_channels { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

		/// <summary>
		/// An array of guilds that this bot is in as incomplete objects.
		/// </summary>
		[JsonProperty("guilds")]
		public UnavailableGuild[] Guilds { get; set; } = new UnavailableGuild[0];

		/// <summary>
		/// The ID of the current session, which is used for resume calls.
		/// </summary>
		[JsonProperty("session_id")]
		public string SessionID { get; set; } = string.Empty;

		/// <summary>
		/// The shard information associated with this session, if sent in the identification packet.<para/>
		/// It will have two values: Its first value is the ID of the shard. The second value is the amount of shards.
		/// </summary>
		[JsonProperty("shard", NullValueHandling = NullValueHandling.Ignore)]
		public int[]? Shard { get; set; }

		public Task Execute(DiscordClient fromClient) => Task.CompletedTask; // handled explicitly

	}
}

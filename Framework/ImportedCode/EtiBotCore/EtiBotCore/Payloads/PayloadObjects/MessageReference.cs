using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// A reference to an original message, used in announcement channels.
	/// </summary>
	internal class MessageReference {

		/// <summary>
		/// The ID of the original message, if applicable.
		/// </summary>
		[JsonProperty("message_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? MessageID { get; set; }

		/// <summary>
		/// The ID of the channel that this message came from.
		/// </summary>
		[JsonProperty("channel_id")]
		public ulong ChannelID { get; set; }

		/// <summary>
		/// The server that this message came from, or <see langword="null"/> if there is no associated guild.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong? GuildID { get; set; }

	}
}

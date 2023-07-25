using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a link specifically to a channel, &lt;#ID&gt;
	/// </summary>
	internal class ChannelMention : PayloadDataObject {

		/// <summary>
		/// The ID of the channel.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// The ID of the server that the channel exists in.
		/// </summary>
		[JsonProperty("guild_id")] 
		public ulong GuildID { get; set; }

		/// <summary>
		/// The type of channel that this is.
		/// </summary>
		[JsonProperty("type")]
		public ChannelType Type { get; set; }

		/// <summary>
		/// The name of this channel.
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; set; } = string.Empty;

	}
}

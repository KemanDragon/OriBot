using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Commands {

	/// <summary>
	/// Joins, moves, or disconnects the client from a voice channel.
	/// </summary>
	class UpdateVoiceStateCommand : PayloadDataObject {

		/// <summary>
		/// The ID of the server that this voice state exists in.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong GuildID { get; set; }

		/// <summary>
		/// The ID of the voice channel to move to, or <see langword="null"/> to disconnect.
		/// </summary>
		[JsonProperty("channel_id")]
		public ulong? ChannelID { get; set; }

		/// <summary>
		/// Whether or not to be muted.
		/// </summary>
		[JsonProperty("self_mute")]
		public bool Mute { get; set; }

		/// <summary>
		/// Whether or not to be deafened.
		/// </summary>
		[JsonProperty("self_deaf")]
		public bool Deafen { get; set; }
	}
}

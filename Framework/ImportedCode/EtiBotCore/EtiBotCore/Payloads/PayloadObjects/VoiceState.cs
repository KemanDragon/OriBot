using EtiBotCore.Data.Structs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a user who is connected to voice chat.
	/// </summary>
	internal class VoiceState : PayloadDataObject {

		/// <summary>
		/// The ID of the server, or <see langword="null"/> if this is a DM call.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong? GuildID { get; set; }

		/// <summary>
		/// The ID of the channel, or <see langword="null"/> if they are not connected.
		/// </summary>
		[JsonProperty("channel_id")]
		public ulong? ChannelID { get; set; }

		/// <summary>
		/// The ID of the user.
		/// </summary>
		[JsonProperty("user_id")]
		public ulong UserID { get; set; }

		/// <summary>
		/// The member that this voice state represents, if this has occurred in a server. <see langword="null"/> for DM calls.
		/// </summary>
		[JsonProperty("member")]
		public Member? Member { get; set; }

		/// <summary>
		/// The ID of this voice session.
		/// </summary>
		[JsonProperty("session_id"), JsonRequired]
		public string SessionID { get; set; } = string.Empty;

		/// <summary>
		/// Whether or not this member is server deafened.
		/// </summary>
		[JsonProperty("deaf")]
		public bool ServerDeafened { get; set; }

		/// <summary>
		/// Whether or not this member is server muted.
		/// </summary>
		[JsonProperty("mute")]
		public bool ServerMuted { get; set; }

		/// <summary>
		/// Whether or not this member has deafened themselves.
		/// </summary>
		[JsonProperty("self_deaf")]
		public bool Deafened { get; set; }

		/// <summary>
		/// Whether or not this member has muted themselves.
		/// </summary>
		[JsonProperty("self_mute")]
		public bool Muted { get; set; }

		/// <summary>
		/// Whether or not this member is using "Go Live". <see langword="null"/> for DM calls.
		/// </summary>
		[JsonProperty("self_stream", NullValueHandling = NullValueHandling.Ignore)]
		public bool? Streaming { get; set; }

		/// <summary>
		/// Whether or not this member has their webcam on.
		/// </summary>
		[JsonProperty("self_video")]
		public bool WebcamOn { get; set; }

		/// <summary>
		/// Whether or not this member us muted by me.
		/// </summary>
		[JsonProperty("suppress")]
		public bool Suppressed { get; set; }

		/// <summary>
		/// When this user last requested to speak.
		/// </summary>
		[JsonProperty("request_to_speak_timestamp")]
		public ISO8601 RequestedToSpeakAt { get; set; } = ISO8601.Epoch;

	}
}

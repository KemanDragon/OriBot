using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Data.Structs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.PayloadObjects {
	internal class ThreadMember : PayloadDataObject {

		/// <summary>
		/// The ID of the thread. Not included if this was received in a GUILD_CREATE event.
		/// </summary>
		[JsonProperty("id")]
		public ulong? ThreadID { get; set; }

		/// <summary>
		/// The ID of the member. Not included if this was received in a GUILD_CREATE event.
		/// </summary>
		[JsonProperty("user_id")]
		public ulong? UserID { get; set; }

		/// <summary>
		/// When this member joined the thread. If they left and rejoined, this is the latest join time.
		/// </summary>
		[JsonProperty("join_timestamp"), JsonConverter(typeof(TimestampConverter))]
		public ISO8601 JoinTimestamp { get; set; }

		[JsonProperty("member")]
		public Member? GuildMember { get; set; }

		[JsonProperty("presence")]
		public Presence? Presence { get; set; }

		/// <summary>
		/// Irrelevant to bots. User-thread settings for notifications.
		/// </summary>
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0169
		private int flags;
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CS0169
	}
}

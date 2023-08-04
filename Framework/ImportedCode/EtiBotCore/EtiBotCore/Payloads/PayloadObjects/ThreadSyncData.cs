using EtiBotCore.Data.Structs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.PayloadObjects {
	internal class ThreadSyncData : PayloadDataObject {

		/// <summary>
		/// The ID of the server
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong ServerID { get; set; }

		/// <summary>
		/// The parent channel IDs whose threads are being synced, or null for every single thread in the entire server. This may also contain
		/// channels without any threads, from which you should use this to dispose of the data.
		/// </summary>
		[JsonProperty("channel_ids")]
		public ulong[]? UpdatedParents { get; set; }

		/// <summary>
		/// All active threads in the given channels (<see cref="UpdatedParents"/>) that this user can access.
		/// </summary>
		[JsonProperty("threads")]
		public Channel[] Threads { get; set; } = new Channel[0];

		/// <summary>
		/// All thread member objects representing the current user, which indicate the threads this user has been added to.
		/// </summary>
		[JsonProperty("members")]
		public ThreadMember[] Members { get; set; } = new ThreadMember[0];

	}
}

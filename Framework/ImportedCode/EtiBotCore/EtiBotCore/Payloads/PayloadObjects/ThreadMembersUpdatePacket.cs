using EtiBotCore.Data.Structs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.PayloadObjects {
	internal class ThreadMembersUpdatePacket : PayloadDataObject {

		/// <summary>
		/// The ID of the thread.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// The ID of the server this thread exists in.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong ServerID { get; set; }

		/// <summary>
		/// The thread members that were added to the thread.
		/// </summary>
		[JsonProperty("added_members")]
		public ThreadMember[] AddedMembers { get; set; } = new ThreadMember[0];

		/// <summary>
		/// The IDs of all members that were removed from this thread.
		/// </summary>
		[JsonProperty("removed_member_ids")]
		public ulong[] RemovedMemberIDs { get; set; } = new ulong[0];

	}
}

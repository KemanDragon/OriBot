using EtiBotCore.Payloads.PayloadObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Intents.GuildBans {

	/// <summary>
	/// Since both the ban add and remove events share a structure, this provides the structure to both of them.
	/// </summary>
	internal class GuildGenericBanEvent : PayloadDataObject {

		/// <summary>
		/// The ID of the server that this ban or pardon occurred in.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong GuildID { get; set; }

		/// <summary>
		/// The user that was affected.
		/// </summary>
		[JsonProperty("user"), JsonRequired]
		public User User { get; set; } = new User();

	}
}

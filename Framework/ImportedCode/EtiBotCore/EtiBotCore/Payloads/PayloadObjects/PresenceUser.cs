using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// A bare-bones user seen in presence objects. It only contains the user's ID.
	/// </summary>
	internal class PresenceUser : PayloadDataObject {

		/// <summary>
		/// The user's unique ID.
		/// </summary>
		[JsonProperty("id")]
		public ulong UserID { get; set; }

	}
}

using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// A message activity.
	/// </summary>
	internal class MessageActivity {

		/// <summary>
		/// The type of message that this is.
		/// </summary>
		[JsonProperty("type"), JsonConverter(typeof(EnumConverter))]
		public MessageActivityType Type { get; set; }

		/// <summary>
		/// The ID of the party.
		/// </summary>
		[JsonProperty("party_id", NullValueHandling = NullValueHandling.Ignore)]
		public string? PartyID { get; set; }

	}
}

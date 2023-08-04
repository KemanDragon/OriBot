using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects.ActivityObjects {
	/// <summary>
	/// Information about the current party.
	/// </summary>
	public class PartyObject {

		internal PartyObject() { }

		/// <summary>
		/// The ID of the party.
		/// </summary>
		[JsonProperty("id")]
		public string ID { get; internal set; } = "";

		/// <summary>
		/// The size of the party, <c>[current size, max size]</c>
		/// </summary>
		[JsonProperty("size")]
		public int[] Size { get; internal set; } = new int[2];

	}
}

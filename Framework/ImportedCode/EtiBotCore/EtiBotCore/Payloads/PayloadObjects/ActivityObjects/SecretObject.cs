using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects.ActivityObjects {
	/// <summary>
	/// Secrets for the <see cref="Activity"/>
	/// </summary>
	[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
	public class SecretObject {

		internal SecretObject() { }

		/// <summary>
		/// The secret needed to join.
		/// </summary>
		[JsonProperty("join")]
		public string? Join { get; internal set; }

		/// <summary>
		/// The secret needed to spectate.
		/// </summary>
		[JsonProperty("spectate")]
		public string? Spectate { get; internal set; }

		/// <summary>
		/// The secret referring to the current match.
		/// </summary>
		[JsonProperty("match")]
		public string? Match { get; internal set; }

	}
}

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects.ActivityObjects {

	/// <summary>
	/// Represents a timestamp for an <see cref="Activity"/>.
	/// </summary>
	[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
	public class TimestampObject {

		internal TimestampObject() { }

		/// <summary>
		/// The time at which this activity started.
		/// </summary>
		[JsonProperty("start")]
		public long? Start { get; internal set; }

		/// <summary>
		/// The time at which this activity will end.
		/// </summary>
		[JsonProperty("end")]
		public long? End { get; internal set; }
	}
}

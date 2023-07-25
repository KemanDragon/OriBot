using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Used to identify the source of the connection to Discord
	/// </summary>
	internal class IdentifyConnectionProperties {

		/// <summary>
		/// The OS this system is running on.
		/// </summary>
		[JsonProperty("$os")]
		public string OS { get; set; } = "Windows";

		/// <summary>
		/// The library this is running on.
		/// </summary>
		[JsonProperty("$browser")]
		public string Browser { get; set; } = "EtiBotCore";

		/// <summary>
		/// The library this is running on.
		/// </summary>
		[JsonProperty("$device")]
		public string Device { get; set; } = "EtiBotCore";

	}
}

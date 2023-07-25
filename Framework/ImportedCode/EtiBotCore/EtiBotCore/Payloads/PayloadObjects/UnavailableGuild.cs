using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a partial guild object which is an offline guild or a guild that has not been provided through the gateway connect guild create events.
	/// </summary>
	internal class UnavailableGuild : PayloadDataObject {

		/// <summary>
		/// The ID of this guild.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// Whether or not this guild object is unavailable.
		/// </summary>
		[JsonProperty("unavailable")]
		public bool Unavailable { get; set; }

	}
}

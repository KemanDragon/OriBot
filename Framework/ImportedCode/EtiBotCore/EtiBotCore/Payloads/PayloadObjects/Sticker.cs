using EtiBotCore.Data.Structs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a sticker item.
	/// </summary>
	internal class Sticker {

		/// <summary>
		/// The ID of this sticker.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// The name of this sticker
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// The type of file this sticker uses.
		/// </summary>
		[JsonProperty("format_type")]
		public int Format { get; set; }

	}
}

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// An application embedded in a message.
	/// </summary>
	internal class MessageApplication {

		/// <summary>
		/// The ID of the application.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// ID of the embed's image asset.
		/// </summary>
		[JsonProperty("cover_image", NullValueHandling = NullValueHandling.Ignore)]
		public string? CoverImage { get; set; }

		/// <summary>
		/// The description of this application.
		/// </summary>
		[JsonProperty("description"), JsonRequired]
		public string Description { get; set; } = string.Empty;

		/// <summary>
		/// The ID of this application's icon.
		/// </summary>
		[JsonProperty("icon")]
		public string? Icon { get; set; }

		/// <summary>
		/// The name of this application.
		/// </summary>
		[JsonProperty("name"), JsonRequired]
		public string Name { get; set; } = string.Empty;


	}
}

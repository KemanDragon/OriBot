using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects.ActivityObjects {

	/// <summary>
	/// The large/small image assets for an <see cref="Activity"/>, and their tooltips.
	/// </summary>
	[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
	public class ActivityAssets {

		internal ActivityAssets() { }

		/// <summary>
		/// The key of the large image.
		/// </summary>
		[JsonProperty("large_image")]
		public string? LargeImageKey { get; internal set; }

		/// <summary>
		/// The tooltip when the large image is hovered.
		/// </summary>
		[JsonProperty("large_text")]
		public string? LargeImageText { get; internal set; }

		/// <summary>
		/// The key of the small image.
		/// </summary>
		[JsonProperty("small_image")]
		public string? SmallImageKey { get; internal set; }

		/// <summary>
		/// The tooltip when the small image is hovered.
		/// </summary>
		[JsonProperty("small_text")]
		public string? SmallImageText { get; internal set; }

	}
}

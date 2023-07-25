using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects.ActivityObjects {
	/// <summary>
	/// A lightweight emoji representation for use in statuses.
	/// </summary>
	[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
	public class EmojiObject {

		internal EmojiObject() { }

		/// <summary>
		/// The name of the emoji.
		/// </summary>
		[JsonProperty("name"), JsonRequired]
		public string Name { get; internal set; } = string.Empty;

		/// <summary>
		/// The ID of the emoji.
		/// </summary>
		[JsonProperty("id")]
		public ulong? ID { get; internal set; }

		/// <summary>
		/// Whether or not this emoji is animated.
		/// </summary>
		[JsonProperty("animated")]
		public bool? Animated { get; internal set; }

	}
}

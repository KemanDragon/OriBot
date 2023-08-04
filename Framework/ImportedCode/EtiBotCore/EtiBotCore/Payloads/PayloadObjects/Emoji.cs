using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents an emoji.
	/// </summary>
	internal class Emoji : PayloadDataObject {
		
		/// <summary>
		/// The ID of this emoji, or <see langword="null"/> if this is a unicode emoji.
		/// </summary>
		[JsonProperty("id")]
		public ulong? ID { get; set; }

		/// <summary>
		/// The name of this emoji. Can be <see langword="null"/> in reaction emoji objects, but only if it's a custom emoji and it was deleted from the guild (from which case ID will be used to identify it)
		/// </summary>
		[JsonProperty("name")]
		public string? Name { get; set; }

		/// <summary>
		/// The IDs of the roles that are allowed to use this emoji (as strings), or <see langword="null"/> if this is not applicable.
		/// </summary>
		[JsonProperty("roles")]
		public string[]? Roles { get; set; }

		/// <summary>
		/// The user that uploaded this emoji, or <see langword="null"/> if it is a stock emoji.
		/// </summary>
		[JsonProperty("user")]
		public User? Creator { get; set; }

		/// <summary>
		/// Whether or not this emoji must be surrounded in colons to use, or <see langword="null"/> if this is not applicable.
		/// </summary>
		[JsonProperty("require_colons", NullValueHandling = NullValueHandling.Ignore)]
		public bool? RequiresColons { get; set; }

		/// <summary>
		/// Whether or not this emoji is managed by an integration, or <see langword="null"/> if this is not applicable.
		/// </summary>
		[JsonProperty("managed", NullValueHandling = NullValueHandling.Ignore)]
		public bool? Managed { get; set; }

		/// <summary>
		/// Whether or not this emoji is animated, or <see langword="null"/> if this is not applicable.
		/// </summary>
		[JsonProperty("animated", NullValueHandling = NullValueHandling.Ignore)]
		public bool? Animated { get; set; }

		/// <summary>
		/// Whether or not this emoji is usable right now (which may be false if a boost is lost, for instance), or <see langword="null"/> if this is not applicable.
		/// </summary>
		[JsonProperty("available", NullValueHandling = NullValueHandling.Ignore)]
		public bool? Available { get; set; }

	}
}

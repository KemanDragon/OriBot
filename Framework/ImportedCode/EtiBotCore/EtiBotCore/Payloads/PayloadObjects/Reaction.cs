using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// A reaction on a message.
	/// </summary>
	internal class Reaction : PayloadDataObject {

		/// <summary>
		/// The amount of times this reaction has been added.
		/// </summary>
		[JsonProperty("count")] 
		public int Count { get; set; }

		/// <summary>
		/// Whether or not the current user reacted with this emoji.
		/// </summary>
		[JsonProperty("me")]
		public bool Me { get; set; }

		/// <summary>
		/// A partial <see cref="Emoji"/> object.
		/// </summary>
		[JsonProperty("emoji"), JsonRequired]
		public Emoji Emoji { get; set; } = new Emoji();

	}
}

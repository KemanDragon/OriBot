using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents an attachment to a message.
	/// </summary>
	internal class Attachment : PayloadDataObject {

		/// <summary>
		/// The ID of this attachment.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// The name of the file in this attachment.
		/// </summary>
		[JsonProperty("filename"), JsonRequired]
		public string FileName { get; set; } = string.Empty;

		/// <summary>
		/// The size of this attachment in bytes.
		/// </summary>
		[JsonProperty("size")]
		public int Size { get; set; }

		/// <summary>
		/// The URL linking to this attachment.
		/// </summary>
		[JsonProperty("url"), JsonRequired]
		public string URL { get; set; } = string.Empty;

		/// <summary>
		/// Alternative, proxied variant of <see cref="URL"/>.
		/// </summary>
		[JsonProperty("proxy_url"), JsonRequired]
		public string ProxyURL { get; set; } = string.Empty;

		/// <summary>
		/// The height of this attachment if it is an image, or <see langword="null"/> otherwise.
		/// </summary>
		[JsonProperty("height")]
		public int? Height { get; set; }

		/// <summary>
		/// The width of this attachment if it is an image, or <see langword="null"/> otherwise.
		/// </summary>
		[JsonProperty("width")]
		public int? Width { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Utility.Attributes;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// Represents a type of embed.<para/>
	/// <strong>Note:</strong> These are deprecated by Discord but must be implemented for API compliance.
	/// </summary>
	[ConvertEnumByName]
	public enum EmbedType {

		/// <summary>
		/// Generic embed rendered via its attributes.
		/// </summary>
		[EnumConversionName("rich")]
		Rich,

		/// <summary>
		/// An image embed.
		/// </summary>
		[EnumConversionName("image")]
		Image,

		/// <summary>
		/// A video embed.
		/// </summary>
		[EnumConversionName("video")]
		Video,

		/// <summary>
		/// A gif embed, rendered as a video.
		/// </summary>
		[EnumConversionName("gifv")]
		GifAsVideo,

		/// <summary>
		/// An article from a website.
		/// </summary>
		[EnumConversionName("article")]
		Article,

		/// <summary>
		/// A link
		/// </summary>
		[EnumConversionName("link")]
		Link

	}
}

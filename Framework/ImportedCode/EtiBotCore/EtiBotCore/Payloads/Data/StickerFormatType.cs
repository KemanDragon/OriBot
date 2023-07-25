using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// Represents the formats used for stickers.
	/// </summary>
	public enum StickerFormatType {

		/// <summary>
		/// This sticker is a PNG file.
		/// </summary>
		PNG = 1,

		/// <summary>
		/// This sticker is an Animated PNG file.
		/// </summary>
		APNG = 2,
		
		/// <summary>
		/// Your guess is as good as mine.
		/// </summary>
		LOTTIE = 3

	}
}

using System;
using System.Collections.Generic;
using System.Text;

namespace OldOriBot.Utility.Formatting {

	/// <summary>
	/// A markdown link.
	/// </summary>
	public class MDLink {

		/// <summary>
		/// The display text.
		/// </summary>
		public string Display { get; set; }

		/// <summary>
		/// The link to go to.
		/// </summary>
		public Uri Link { get; set; }

		/// <summary>
		/// Construct a new markdown link with the given display text and URL.
		/// </summary>
		/// <param name="display"></param>
		/// <param name="link"></param>
		public MDLink(string display, string link) {
			Display = display;
			Link = new Uri(link);
		}

		/// <summary>
		/// Returns the markdown-formatted link [Display](Link)
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return $"[{Display}]({Link})";
		}

	}
}

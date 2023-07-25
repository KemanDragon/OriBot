using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.Exceptions;

namespace OldOriBot.Data.Commands.ArgData {

	/// <summary>
	/// Represents a color that a user can input into a command.
	/// </summary>

	[Serializable]
	public class Color : ICommandArg<Color> {

		public Color() { }

		/// <summary>
		/// The RGB color of this <see cref="Color"/> as an integer.
		/// </summary>
		public int Value { get; }

		private Color(string data) {
			// Hex color matching, with an optional leading #:
			// Mostly looks for 6 hex digits in a row.
			Match hexMatch = Regex.Match(data, @"#*([0-9]|[a-f]|[A-F]){6}");
			if (hexMatch.Success) {
				// friendly self-reminder that Groups[0] is the entire match, not one of the capture groups
				string hexCode = hexMatch.Groups[0].Value;
				if (hexCode[0] == '#') hexCode = hexCode[1..];
				Value = Convert.ToInt32(hexCode, 16);
				return;
			}

			MatchCollection rgbMatches = Regex.Matches(data, @"(\d{1,3})(,*\s*)");
			if (rgbMatches.Count == 3) {
				Match red = rgbMatches[0];
				Match green = rgbMatches[1];
				Match blue = rgbMatches[2];
				byte r = Convert.ToByte(red.Groups[1].Value);
				byte g = Convert.ToByte(green.Groups[1].Value);
				byte b = Convert.ToByte(blue.Groups[1].Value);

				// 0x00RRGGBB
				Value = (r << 16) | (g << 8) | b;
			} else if (rgbMatches.Count != 0) {
				throw new FormatException("Attempted to parse an RGB color code separated by commas, but I didn't find 3 values! Expected `r,g,b`", new NoThrowDummyException());
			}

			throw new FormatException("Unable to convert the given input into a color.", new NoThrowDummyException());
		}

		public Color From(string instance, object inContext) {
			return new Color(instance);
		}

		object ICommandArg.From(string instance, object inContext) => ((ICommandArg<Color>)this).From(instance, inContext);
	}
}

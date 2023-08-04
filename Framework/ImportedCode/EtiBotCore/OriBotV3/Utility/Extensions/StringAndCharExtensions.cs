using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace OldOriBot.Utility.Extensions {
	public static class StringAndCharExtensions {

		/// <summary>
		/// Adds <paramref name="left"/> to the left of <paramref name="text"/> and <paramref name="right"/> to the right of <paramref name="text"/>.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string SurroundIn(this string text, char left, char right) {
			return left + text + right;
		}

		/// <summary>
		/// Expects <paramref name="lr"/> to be 2 chars. Adds <paramref name="lr"/>[0] to the left of <paramref name="text"/> and <paramref name="lr"/>[1] to the right of <paramref name="text"/>.<para/>
		/// If it is 1 char, it will use that for both the start and end.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="lr"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If the amount of <see langword="char"/>s in <paramref name="lr"/> is not 2.</exception>
		public static string SurroundIn(this string text, params char[] lr) {
			if (lr == null) throw new ArgumentNullException(nameof(lr));
			if (lr.Length == 1) lr = new char[] { lr[0], lr[0] };
			if (lr.Length != 2) throw new ArgumentException($"Expected param {nameof(lr)} to be 1 or 2 chars long!");
			return SurroundIn(text, lr[0], lr[1]);
		}

		/// <summary>
		/// Expects <paramref name="lr"/> to be 2 chars. Adds <paramref name="lr"/>[0] to the left of <paramref name="text"/> and <paramref name="lr"/>[1] to the right of <paramref name="text"/>.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="lr"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If the amount of <see langword="char"/>s in <paramref name="lr"/> is not 2.</exception>
		public static string SurroundIn(this string text, string lr) {
			if (lr == null) throw new ArgumentNullException(nameof(lr));
			if (lr.Length != 2) throw new ArgumentException($"Expected param {nameof(lr)} to be 2 chars long!");
			return SurroundIn(text, lr[0], lr[1]);
		}

		/// <summary>
		/// Surrounds the given elements of <paramref name="text"/> in the first and last chars of <paramref name="srp"/>, and separates them with the characters between the first and last in <paramref name="srp"/>
		/// </summary>
		/// <param name="text"></param>
		/// <param name="srp"></param>
		/// <returns></returns>
		public static string SurroundInAndSplit(this string[] text, string srp) {
			string first = srp.Substring(0, 1);
			string last = srp.Substring(srp.Length - 2, 1);
			string mid = srp[1..^1];
			string res = first;
			for (int i = 0; i < text.Length; i++) {
				res += text[i];
				if (i < text.Length - 1) {
					res += mid;
				}
			}
			return res + last;
		}

		/// <summary>
		/// Escapes all markdown characters in this <see cref="string"/>.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="reverseGraves">If true, an acute accent (´) will be used to replace graves (`) as to retain as similar an appearance as possible</param>
		/// <returns></returns>
		public static string EscapeAllDiscordMarkdown(this string text, bool reverseGraves = true) {
			text = text.Replace("*", "\\*").Replace("_", "\\_").Replace("~", "\\~").Replace("|", "\\|");
			if (reverseGraves) {
				text = text.Replace("`", "´"); // lol
			} else {
				text = text.Replace("`", "\\`");
			}
			return text;
		}

		/// <summary>
		/// Finds all graves (`) in the string and reverses them into acute accents (´) so that it doesn't interfere with Discord's formating.
		/// </summary>
		/// <returns></returns>
		public static string ReverseGraves(this string text) {
			return text.Replace("`", "´");
		}

		/// <summary>
		/// Reverses this <see cref="string"/>.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string Backwards(this string text) {
			if (text == null) return null;
			string t = "";
			for (int idx = text.Length - 1; idx >= 0; idx--) {
				t += text[idx];
			}
			return t;
		}

		/// <summary>
		/// Replaces smart quotation marks (often employed by phones or macbooks) standard quote marks ""
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ReplaceQuotationMarks(this string text) {
			return text.Replace("“", "\"").Replace("”", "\"").Replace("‘", "'").Replace("’", "'");
		}

		/// <summary>
		/// Returns <see langword="true"/> if <paramref name="c"/> is an ASCII character within the range of [32, 127].
		/// </summary>
		/// <param name="c">The <see cref="char"/> to test to see if it is a non-control-code ASCII character.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNonCtrASCII(this char c) {
			return c > 0x1F && c <= 0x7F;
		}

		/// <summary>
		/// Returns <see langword="true"/> if <paramref name="c"/> is an ASCII character within the range of [0, 127].
		/// </summary>
		/// <param name="c">The <see cref="char"/> to test to see if it is an ASCII character.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsASCII(this char c) {
			return c <= 0x7F;
		}

		/// <summary>
		/// Returns the input string with up to <paramref name="nchars"/> characters.
		/// </summary>
		/// <param name="nchars">The maximum amount of chars in this string. Note that if <paramref name="addDots"/> is true, the resulting string may be this long + 3</param>
		/// <param name="addDots">Adds "..." to the end. I don't remember how to spell it. Elipses? Elipsis? ...Yeah I dunno.</param>
		/// <returns></returns>
		public static string LimitCharCount(this string text, int nchars, bool addDots = false) {
			if (text.Length <= nchars) return text;
			string result = text.Substring(0, nchars);
			if (addDots) result += "...";
			return result;
		}

	}
}

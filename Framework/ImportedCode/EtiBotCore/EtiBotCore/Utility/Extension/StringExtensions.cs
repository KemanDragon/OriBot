using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Utility.Extension {

	/// <summary>
	/// Provides extensions to <see cref="string"/>
	/// </summary>
	public static class StringExtensions {

		/// <summary>
		/// Returns the string up to its first null byte (0). Expects the string to be ASCII encoded.
		/// </summary>
		/// <returns></returns>
		public static string GetNullTerminatedString(this Encoding encoding, byte[] data) {
			int idx = 0;
			for ( ; idx < data.Length; idx++) {
				if (data[idx] == 0) break;
			}
			byte[] newData = new byte[idx];
			for (int index = 0; index < idx; index++) {
				newData[index] = data[index];
			}
			return encoding.GetString(newData);
		}

		/// <summary>
		/// Given text, this method will find the first occurrence of <paramref name="start"/> and <paramref name="end"/>.<para/>
		/// If both exist and end is after start, it will return all characters between these two (NOT including them)<para/>
		/// If this case is not met, <see langword="null"/> is returned.<para/>
		/// Example: <c>GetStringBetween("[Hello, world!]", '[', ']')</c> returns <c>Hello, world!</c>
		/// </summary>
		/// <param name="text">The string to search.</param>
		/// <param name="start">The character that marks the start of the result.</param>
		/// <param name="end">The character that marks the end of the result.</param>
		/// <param name="after">Starts the search after this character index.</param>
		/// <returns></returns>
		public static string? GetStringBetween(this string text, char start, char end, int after = 0) {
			if (!text.Contains(start) || !text.Contains(end)) return null;
			int startIdx = text.IndexOf(start, after);
			if (startIdx == -1) return null;
			int endIdx = text.IndexOf(end, Math.Max(after, startIdx));
			if (endIdx == -1) return null;

			int length = endIdx - startIdx;
			return text.Substring(startIdx + 1, length - 1);
		}

		/// <summary>
		/// Given text, this method will find the first occurrence of <paramref name="ch"/>, and the second occurrence after that.<para/>
		/// If both exist, it will return all characters between these two (NOT including them)<para/>
		/// If this case is not met, <see langword="null"/> is returned.<para/>
		/// Example: <c>GetStringBetween(":Hello, world!:", ':')</c> returns <c>Hello, world!</c>
		/// </summary>
		/// <param name="text">The string to search.</param>
		/// <param name="ch">The character that marks the start and end of the result.</param>
		/// <param name="after">Starts the search after this character index.</param>
		/// <returns></returns>
		public static string? GetStringBetween(this string text, char ch, int after = 0) => GetStringBetween(text, ch, ch, after);

		/// <summary>
		/// Checks if <paramref name="start"/> and <paramref name="end"/> are the first and last characters of this string.
		/// </summary>
		/// <param name="text">The text to test.</param>
		/// <param name="start">The character that should be at the start of the string.</param>
		/// <param name="end">The character that should be at the end of the string.</param>
		/// <returns></returns>
		public static bool IsPaddedWith(this string text, char start, char end) {
			if (text == null) return false;
			if (text.Length < 2) return false;
			return text[0] == start && text[^1] == end;
		}

		/// <summary>
		/// Checks if <paramref name="ch"/> makes up the first and last character of this string.
		/// </summary>
		/// <param name="text">The text to test.</param>
		/// <param name="ch">The character that should be at the start and end of this string.</param>
		/// <returns></returns>
		public static bool IsPaddedWith(this string text, char ch) => IsPaddedWith(text, ch, ch);
	}
}

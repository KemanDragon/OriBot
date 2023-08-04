using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmojiLookupTool {

	/// <summary>
	/// Provides a means of acquiring a unicode emoji from its CLDR name. This is acquired from unicode.
	/// </summary>
	public static class EmojiLookup {
		// # Format: code points; status # emoji name

		/// <summary>
		/// A binding from emoji name (e.g. <c>:slight_smile:</c>) to its corresponding emoji 🙂
		/// </summary>
		public static readonly IReadOnlyDictionary<string, string> EmojiNameToEmoji;

		/// <summary>
		/// Using an emoji name (e.g. <c>:slight_smile:</c>) this will return its corresponding emoji 🙂<para/>
		/// If the surrounding :s are not provided, they will be added.
		/// </summary>
		/// <remarks>
		/// This is identical to directly referencing <see cref="EmojiNameToEmoji"/>, with the exception that it will return null instead of error if a name is invalid.
		/// </remarks>
		/// <param name="name"></param>
		public static string? GetEmoji(string name) {
			// Remove any surrounding :s
			if (name[0] == ':') name = name[1..];
			if (name[^1] == ':') name = name[..^1];

			// Get emoji
			if (EmojiNameToEmoji.TryGetValue(name, out string? emoji)) {
				return emoji;
			}
			return null;
		}

		private static string[] GetEmojiDefinitions() {
			if (File.Exists(@".\emoji-test.txt")) {
				return GetEmojiText();
			}
			using WebClient client = new WebClient();
			string emojiTest = client.DownloadString(new Uri("https://unicode.org/Public/emoji/13.1/emoji-test.txt"));
			File.WriteAllText(@".\emoji-test.txt", emojiTest);
			return GetEmojiText();
		}

		private static string[] GetEmojiText() {
			return File.ReadAllLines(@".\emoji-test.txt");
		}

		static EmojiLookup() {
			string[] emojiGarbage = GetEmojiDefinitions();
			Dictionary<string, string> bindings = new Dictionary<string, string>();
			for (int idx = 0; idx < emojiGarbage.Length; idx++) {
				string line = emojiGarbage[idx];
				if (line.StartsWith('#')) continue;
				string[] info = line.Split(new char[] { '#' }, 2);
				if (info.Length < 2) continue;
				if (!info[0].Contains("fully-qualified")) continue;

				string data = info[1][1..];
				Regex.Replace(data, @" E\d+\.\d+ ", "|");
				string[] thajuice = data.Split('|');
				string emoji = thajuice[0];
				string name = thajuice[1];
				if (bindings.ContainsKey(name)) continue; // Only happens for qualified names, I try to filter those out above.
				bindings[name] = emoji;
			}
			EmojiNameToEmoji = bindings;
		}
	}
}

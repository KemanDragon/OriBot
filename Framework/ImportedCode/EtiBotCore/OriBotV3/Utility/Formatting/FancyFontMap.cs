using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EtiLogger.Logging;
using OldOriBot.Utility.Extensions;

namespace OldOriBot.Utility.Formatting {
	public static class FancyFontMap {

		/// <summary>
		/// The English alphabet in order from a-z then A-Z.
		/// </summary>
		public const string ALPHABET_LOWER_AND_UPPER = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

		/// <summary>
		/// The English alphabet in order from a-z then A-Z, but upside down.
		/// </summary>
		public const string ALPHABET_UPSIDE_DOWN_LOWER_AND_UPPER = "ɐqɔpǝɟƃɥᴉɾʞlɯuodbɹsʇnʌʍxʎz∀qƆpƎℲפHIſʞ˥WNOԀQɹS┴∩ΛMX⅄Z";

		/// <summary>
		/// The letters in <see cref="ALPHABET_UPSIDE_DOWN_LOWER_AND_UPPER"/> that are not unique chars and are instead ordinary chars.
		/// </summary>
		public const string UPSIDE_DOWN_EXCLUSION_CHARS = "qpluodbsnxzqpHIWNOQSMXZ";

		/// <summary>
		/// All glyphs. This is the raw content of the file.
		/// </summary>
		[Obsolete] public static readonly string AllGlyphs;

		/// <summary>
		/// All glyph characters, which may contain more than one <see cref="char"/> per character.
		/// </summary>
		public static readonly List<string> GlyphChars = new List<string>();

		private static readonly List<char> ZalgoText = new List<char>();

		public static readonly IReadOnlyDictionary<char, string[]> LettersToFancyVariantsMap;

		static FancyFontMap() {
			/*
			string fancyStuffs;
			if (Directory.Exists("V:\\")) {
				fancyStuffs = File.ReadAllText(@"V:\EtiBotCore\AllFancyFontGlyphs.txt");
			} else {
				fancyStuffs = File.ReadAllText(@"C:\EtiBotCore\AllFancyFontGlyphs.txt");
			}
			string[] letters = fancyStuffs.Split(' ');
			AllGlyphs = fancyStuffs;
			GlyphChars = letters;
			*/

			string path = @"C:\EtiBotCore\FancyFontGlyphStorage\";
			if (Directory.Exists("V:\\")) {
				path = @"V:\EtiBotCore\FancyFontGlyphStorage\";
			}

			Dictionary<char, string[]> data = new Dictionary<char, string[]>();
			for (int idx = 0; idx < 26; idx++) {
				char letter = ALPHABET_LOWER_AND_UPPER[idx];
				string fancyStuffs = File.ReadAllText(path + letter + ".txt");
				data[letter] = fancyStuffs.Split(' ');
				GlyphChars.AddRange(data[letter]);
			}

			LettersToFancyVariantsMap = data;

			for (int i = 768; i <= 865; i++) {
				if (i == 847) continue;
				ZalgoText.Add((char)i);
			}
		}

		/// <summary>
		/// Attempts to convert special characters into their ASCII counterparts. Does nothing if the name has no special characters.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static string Convert(string name) {
			try {
				if (NameContainsUpsideDown(name)) {
					name = name.Backwards();
					string newName = "";
					for (int idx = 0; idx < name.Length; idx++) {
						// Not gonna do char indices due to non-char alignment.
						string currentChr = name.Substring(idx, 1);
						if (ALPHABET_UPSIDE_DOWN_LOWER_AND_UPPER.Contains(currentChr)) {
							// It's upside down and it's not in the exclusion list.
							// Edit: Screw the exclusion list here. I use it when testing the overall bool since I don't want to make false positives on normal names.
							// If they have upside down chars, they're probably using the normal ones as flips.
							int i = ALPHABET_UPSIDE_DOWN_LOWER_AND_UPPER.IndexOf(currentChr);
							currentChr = ALPHABET_LOWER_AND_UPPER.Substring(i, 1);
						}
						newName += currentChr;
					}
					name = newName;
				}

				for (int set = 0; set < 26; set++) {
					char replacement = ALPHABET_LOWER_AND_UPPER[set];
					foreach (string fancyChar in LettersToFancyVariantsMap[replacement]) {
						if (HasNonASCIIChars(fancyChar)) {
							// The test above is done because some of these fancy fonts end up using standard ASCII letters anyway,
							// so we don't want to replace if the substitution is literally doing nothing.
							// This also fixes a bug where certain letters wouldn't work and threw an error in usernames.
							name = name.Replace(fancyChar, replacement.ToString());
						}
					}
				}

				foreach (char c in ZalgoText) {
					name = name.Replace(c.ToString(), "");
				}
			} catch (Exception ex) {
				Logger.Default.WriteException(ex);
			}
			return name;
		}

		public static double GetPercentZalgoText(string str) {
			double max = str.Length;
			double current = 0;
			foreach (char c in str) {
				if (ZalgoText.Contains(c)) current++;
			}
			return current / max;
		}

		/// <summary>
		/// Attempts to convert special characters into their ASCII counterparts. Does nothing if the name has no special characters.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private static string Convert_(string name) {
			try {
				int setCount = GlyphChars.Count / 52;
				int at = 0;

				if (NameContainsUpsideDown(name)) {
					name = name.Backwards();
					string newName = "";
					for (int idx = 0; idx < name.Length; idx++) {
						// Not gonna do char indices due to non-char alignment.
						string currentChr = name.Substring(idx, 1);
						if (ALPHABET_UPSIDE_DOWN_LOWER_AND_UPPER.Contains(currentChr)) {
							// It's upside down and it's not in the exclusion list.
							// Edit: Screw the exclusion list here. I use it when testing the overall bool since I don't want to make false positives on normal names.
							// If they have upside down chars, they're probably using the normal ones as flips.
							int i = ALPHABET_UPSIDE_DOWN_LOWER_AND_UPPER.IndexOf(currentChr);
							currentChr = ALPHABET_LOWER_AND_UPPER.Substring(i, 1);
						}
						newName += currentChr;
					}
					name = newName;
				}

				for (int set = 0; set < setCount; set++) {
					for (int i = 0; i < 52; i++) {
						// 52 for 26+26, a-z + A-Z
						string chr = GlyphChars[at];
						if (HasNonASCIIChars(chr)) {
							// The test above is done because some of these fancy fonts end up using standard ASCII letters anyway,
							// so we don't want to replace if the substitution is literally doing nothing.
							// This also fixes a bug where certain letters wouldn't work and threw an error in usernames.
							string replacement = ALPHABET_LOWER_AND_UPPER[i].ToString();
							name = name.Replace(chr, replacement);
							at++;
						}
						// XanBotLogger.WriteDebugLine("Replaced all instances of " + chr + " with " + replacement);
					}
				}
				return name;
			} catch (Exception ex) {
				Logger.Default.WriteException(ex);
			}
			return name;
		}

		/// <summary>
		/// Returns true if the name has any chars with a value &gt; 127
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static bool HasNonASCIIChars(string name) {
			foreach (char c in name) {
				if (c > 127) {
					return true;
				}
			}
			return false;
		}

		public static bool NameHasKnownUnwantedChars(string name) {
			foreach (string chr in GlyphChars) {
				if (chr.Length == 1 && chr[0] < 128) continue; // This is an ASCII character. Ignore it.
				if (name.Contains(chr)) return true;
			}
			return false;
		}

		/// <summary>
		/// Intended to be synchronized with Ori Discord server rule #4 which outlines "names must have at least 4 consecutive letters in the ASCII range."
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static bool NameHasFourOKCharsInARow(string name) {
			int consec = 0;
			foreach (char c in name) {
				if (c.IsNonCtrASCII()) {
					// If the character is not a control code and is in the ASCII range...
					consec++;
					if (consec == 4) {
						return true; // definitely OK
					}
				} else {
					consec = 0;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns true if the given text contains upside down characters.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static bool NameContainsUpsideDown(string name) {
			foreach (char c in name) {
				//ɐqɔpǝɟƃɥᴉɾʞlɯuodbɹsʇnʌʍxʎz∀qƆpƎℲפHIſʞ˥WNOԀQɹS┴∩ΛMX⅄Z
				if (ALPHABET_UPSIDE_DOWN_LOWER_AND_UPPER.Contains(c) && !UPSIDE_DOWN_EXCLUSION_CHARS.Contains(c)) {
					return true;
				}
			}
			return false;
		}

	}
}

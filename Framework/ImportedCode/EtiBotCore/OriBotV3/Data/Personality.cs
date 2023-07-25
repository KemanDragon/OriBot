using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using EtiBotCore.Utility.Extension;
using EtiLogger.Data.Structs;
using EtiLogger.Logging;

namespace OldOriBot.Data {

	/// <summary>
	/// Allows defining a "flavor" for various responses.
	/// </summary>
	public class Personality {

		public static string DefaultPath {
			get {
				if (_DefaultPath == null) {
					if (Directory.Exists("V:\\")) {
						_DefaultPath = @"V:\EtiBotCore\Default.personality";
					} else {
						_DefaultPath = @"C:\EtiBotCore\Default.personality";
					}
				}
				return _DefaultPath;
			}
		}
		private static string _DefaultPath = null;

		private static readonly Logger PersonalityLogger = new Logger(new LogMessage.MessageComponent("[Personality Loader] ", new Color(0, 63, 255))) {
			NoLevel = true
		};

		/// <summary>
		/// The defautl personality instance.
		/// </summary>
		public static readonly Personality Default = new Personality(DefaultPath);

		/// <summary>
		/// The current personality instance that is being used.
		/// </summary>
		/// <exception cref="ArgumentNullException">If this is set to null.</exception>
		public static Personality Current {
			get => _Current;
			set => _Current = value ?? throw new ArgumentNullException(nameof(value));
		}
		private static Personality _Current;

		private Dictionary<string, string> ResponseArray = new Dictionary<string, string>();

		/// <summary>
		/// Whether or not this is the default <see cref="Personality"/>
		/// </summary>
		public bool IsDefault { get; }

		/// <summary>
		/// The file this loaded from.
		/// </summary>
		private FileInfo Source { get; }

		/// <summary>
		/// Construct a new <see cref="Personality"/> from the given file, which should contain keys and their associated values.
		/// </summary>
		/// <param name="filePath"></param>
		public Personality(string filePath) {
			IsDefault = filePath == DefaultPath;
			if (IsDefault) _Current = this;
			FileInfo file = new FileInfo(filePath);
			Source = file;
			string[] lines = File.ReadAllLines(file.FullName);
			// First things first: strip whitespace.
			int lineNumber = 1;
			foreach (string l in lines) {
				string line = Regex.Replace(l, @"#.+", "");
				if (string.IsNullOrWhiteSpace(line)) {
					lineNumber++;
					continue;
				}// Nothing was left behind.
				string withoutWhitespace = Regex.Replace(line, @"\s{2,}", "");
				
				Match m = Regex.Match(line, @"(\S*\w+)(=)(.+)");
				if (m.Success) {
					/*
					if (m.Groups[0].Value != withoutWhitespace) {
						PersonalityLogger.WriteLine(m.Groups[0].ToString());
						PersonalityLogger.WriteLine(withoutWhitespace);
						FormatException exc = new FormatException($"Invalid formatting on line {lineNumber} of personality file {file.FullName}.");
						PersonalityLogger.WriteUnthrownException(exc, true);
						break;
					}
					*/
					string key = m.Groups[1].Value;
					string value = m.Groups[3].Value;
					value = value.Replace("\\n", "\n").Replace("\\t", "\t");
					ResponseArray[key] = value;
					PersonalityLogger.WriteLine($"§fSet key §7{key}§8 to §7{value}§f", LogLevel.Trace);
				}
				lineNumber++;
			}
		}

		/// <summary>
		/// Attempts to get the corresponding entry for the given key. If it doesn't exist, it will search <see cref="Default"/>. If it doesn't exist there either, the key itself is returned followed by all input params.
		/// </summary>
		/// <param name="key">The entry key.</param>
		/// <returns></returns>
		public string GetEntry(string key, params object[] format) {
			if (ResponseArray.TryGetValue(key, out string response)) {
				try {
					return string.Format(response, format);
				} catch {
					string retVal = response + "{ ";
					foreach (object o in format) {
						retVal += o.ToString() + " ";
					}
					return retVal + "}";
				}
			}
			if (!IsDefault) {
				return Default.GetEntry(key);
			}
			string ret = key + "{ ";
			foreach (object o in format) {
				ret += o.ToString() + " ";
			}
			return ret + "}";
		}

		/// <summary>
		/// Reloads this personality from disk.
		/// </summary>
		public void Reload() {
			string[] lines = File.ReadAllLines(Source.FullName);
			Dictionary<string, string> newRespArray = new Dictionary<string, string>();
			// First things first: strip whitespace.
			int lineNumber = 1;
			foreach (string l in lines) {
				string line = Regex.Replace(l, @"#.+", "");
				if (string.IsNullOrWhiteSpace(line)) {
					lineNumber++;
					continue;
				}// Nothing was left behind.
				string withoutWhitespace = Regex.Replace(line, @"\s{2,}", "");

				Match m = Regex.Match(line, @"(\S*\w+)(=)(.+)");
				if (m.Success) {
					/*
					if (m.Groups[0].Value != withoutWhitespace) {
						PersonalityLogger.WriteLine(m.Groups[0].ToString());
						PersonalityLogger.WriteLine(withoutWhitespace);
						FormatException exc = new FormatException($"Invalid formatting on line {lineNumber} of personality file {file.FullName}.");
						PersonalityLogger.WriteUnthrownException(exc, true);
						break;
					}
					*/
					string key = m.Groups[1].Value;
					string value = m.Groups[3].Value;
					value = value.Replace("\\n", "\n").Replace("\\t", "\t");
					newRespArray[key] = value;
					PersonalityLogger.WriteLine($"§fSet key §7{key}§8 to §7{value}§f", LogLevel.Trace);
				}
				lineNumber++;
			}
			ResponseArray = newRespArray;
		}

		/// <summary>
		/// Attempts to get the corresponding entry for the given key out of <see cref="Current"/>. Identical to calling <see cref="GetEntry(string)"/> on <see cref="Current"/>
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string Get(string key, params object[] format) => Current.GetEntry(key, format);

		public static string GetRandomKuResponse() {
			string stockEntries = Current.GetEntry("RANDOMIZED_ENTRY_KU_APR1");
			string rareEntries = Current.GetEntry("RANDOMIZED_ENTRY_KU_APR1_RARE");

			if (RNG.NextDouble() > 0.005) {
				string[] stock = stockEntries.Split(';');
				return stock.Random();
			} else {
				string[] rare = rareEntries.Split(';');
				return rare.Random();
			}
		}

		private static readonly Random RNG = new Random();

	}
}

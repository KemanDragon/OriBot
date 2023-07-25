using EtiLogger.Data.Structs;
using EtiLogger.Logging.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EtiLogger.Logging.LogMessage;

namespace EtiLogger.Logging {

	/// <summary>
	/// Represents a pre-baked log message for <see cref="Logger"/>, and provides methods of assembling colored log strings.<para/>
	/// Most importantly, it provides a means of turning VT sequences back into <see cref="LogMessage"/> instances.
	/// </summary>
	public sealed class LogMessage {

		#region Constants
		/// <summary>
		/// The symbol used for switching to legacy console color codes.
		/// </summary>
		public const char LEGACY_COLOR_CODE_SYM = '§';

		/// <summary>
		/// The symbol used to signify that the formatting of text will change. 
		/// </summary>
		public const char TEXT_CODE_SYM = '^';

		/// <summary>
		/// The default color to use if no color is defined for text.
		/// </summary>
		public static readonly Color DEFAULT_COLOR = new Color(255, 255, 255);

		/// <summary>
		/// The default color to use if no color is defined for the background
		/// </summary>
		public static readonly Color DEFAULT_BACKGROUND_COLOR = new Color(0, 0, 0);
		#endregion

		#region Maps
		/// <summary>
		/// A map of byte code values to ConsoleColors
		/// </summary>
		private static readonly IReadOnlyDictionary<byte, ConsoleColor> ConsoleColorMap = new Dictionary<byte, ConsoleColor> {
			[0] = ConsoleColor.Black,
			[1] = ConsoleColor.DarkBlue,
			[2] = ConsoleColor.DarkGreen,
			[3] = ConsoleColor.DarkCyan,
			[4] = ConsoleColor.DarkRed,
			[5] = ConsoleColor.DarkMagenta,
			[6] = ConsoleColor.DarkYellow,
			[7] = ConsoleColor.DarkGray,
			[8] = ConsoleColor.Gray,
			[9] = ConsoleColor.Blue,
			[10] = ConsoleColor.Green,
			[11] = ConsoleColor.Cyan,
			[12] = ConsoleColor.Red,
			[13] = ConsoleColor.Magenta,
			[14] = ConsoleColor.Yellow,
			[15] = ConsoleColor.White
		};

		private static readonly IReadOnlyDictionary<ConsoleColor, Color> ConsoleColorToRGBMap = new Dictionary<ConsoleColor, Color> {
			[ConsoleColor.Black] = new Color(0, 0, 0),
			[ConsoleColor.DarkBlue] = new Color(0, 0, 128),
			[ConsoleColor.DarkGreen] = new Color(0, 128, 0),
			[ConsoleColor.DarkCyan] = new Color(0, 128, 128),
			[ConsoleColor.DarkRed] = new Color(128, 0, 0),
			[ConsoleColor.DarkMagenta] = new Color(128, 0, 128),
			[ConsoleColor.DarkYellow] = new Color(128, 128, 0),
			[ConsoleColor.DarkGray] = new Color(128, 128, 128),
			[ConsoleColor.Gray] = new Color(192, 192, 192),
			[ConsoleColor.Blue] = new Color(0, 0, 255),
			[ConsoleColor.Green] = new Color(0, 255, 0),
			[ConsoleColor.Cyan] = new Color(0, 255, 255),
			[ConsoleColor.Red] = new Color(255, 0, 0),
			[ConsoleColor.Magenta] = new Color(255, 0, 255),
			[ConsoleColor.Yellow] = new Color(255, 255, 0),
			[ConsoleColor.White] = new Color(255, 255, 255)
		};
		#endregion

		#region Color Control

		/// <summary>
		/// Given a <see cref="ConsoleColor"/>, this will return the equivalent "starbound color".
		/// </summary>
		/// <param name="conColor"></param>
		/// <returns></returns>
		private static string SBFromConsoleColor(ConsoleColor conColor) {
			Color color = ConsoleColorToRGBMap[conColor];
			return "^#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2") + ";";
		}

		/// <summary>
		/// Translates legacy colors using § into the modern Starbound-style RGB codes.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		private static string TranslateLegacyColors(string message) {
			if (!message.Contains(LEGACY_COLOR_CODE_SYM)) return message;

			string[] colorSegs = message.Split(LEGACY_COLOR_CODE_SYM);
			// colorSegs[0] will be empty if we have a color code at the start.
			string newStr = "";
			foreach (string coloredString in colorSegs) {
				if (coloredString.Length > 1) {
					byte code = 255;
					try { code = Convert.ToByte(coloredString.First().ToString(), 16); } catch (Exception) { }
					bool success = ConsoleColorMap.TryGetValue(code, out ConsoleColor color);
					if (success) {
						// This is a valid color code. Change the color.
						//Target.SelectionColor = ConsoleColorToRGBMap[color];
						newStr += SBFromConsoleColor(color) + coloredString[1..];
					} else {
						newStr += coloredString;
					}

				}
			}
			return newStr;
		}

		#endregion

		#region Constructors & Members

		/// <summary>
		/// All of the <see cref="MessageComponent"/>s that make up this <see cref="LogMessage"/>.
		/// </summary>
		public IReadOnlyList<MessageComponent> Components => ComponentsInternal;

		/// <summary>
		/// Keeps track of all colored pieces of this message.
		/// </summary>
		private readonly List<MessageComponent> ComponentsInternal = new List<MessageComponent>();

		/// <summary>
		/// Create a new <see cref="LogMessage"/>.
		/// </summary>
		public LogMessage() { }

		/// <summary>
		/// Create a new <see cref="LogMessage"/> from the given format string. Formatting methods include:
		/// <list type="bullet">
		/// <item>
		/// <term><em>Minecraft</em> style</term>
		/// <description>
		/// Inspired by <em>Minecraft</em>'s formatting system, a nibble (single hex digit) can be appended after a section symbol (<c>§</c>) to change the color e.g. <c>§a</c> for light green.<para/>
		/// The value corresponds to <see href="https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/color">the Windows Command Prompt's color command</see>.
		/// </description>
		/// </item>
		/// <item>
		/// <term><em>Starbound</em> style</term>
		/// <description>
		/// Inspired by <em>Starbound</em>'s formatting system, a number of operators can be used. Operators start with a caret <c>^</c> and end with a semicolom <c>;</c>.<para/>
		/// Operations are: <c>^b; ^i; ^u; ^s;</c> for bold, italics, underline, and strikethrough respectively. Appending a <c>!</c> after the caret (e.g. <c>^!b;</c>) will disable the effect.<para/>
		/// Colors can be implemented via inputting an HTML-style hex color code, e.g. <c>^#FF0000;</c> will create red. Finally, <c>^r;</c> or <c>^reset;</c> will remove all custom attributes.
		/// </description>
		/// </item>
		/// </list>
		/// </summary>
		public LogMessage(string coloredString) {
			coloredString = TranslateLegacyColors(coloredString);
			string[] splitByMarkers = coloredString.Split(TEXT_CODE_SYM);
			if (splitByMarkers.Length == 1) {
				AddComponent(new MessageComponent(coloredString));
				return;
			}

			for (int componentID = 0; componentID < splitByMarkers.Length; componentID++) {
				string segment = splitByMarkers[componentID];
				if (segment.StartsWith("#")) {
					int endIdx = segment.IndexOf(';');
					if (endIdx != -1) {
						string colorCmp = segment[1..endIdx];
						if (colorCmp.Length == 6) {
							try {
								int hexColor = Convert.ToInt32(colorCmp, 16);
								AddComponent(new MessageComponent(segment[(endIdx + 1)..], new Color(hexColor)));
								continue;
							} catch { }
						}
					}
				} else if (segment.StartsWith("r;") || segment.StartsWith("reset;")) {
					AddComponent(new MessageComponent(segment[6..], DEFAULT_COLOR, DEFAULT_BACKGROUND_COLOR, false, false, false, false));
					continue;
				} else {
					bool state = true;
					string newSegment = segment;
					if (segment.StartsWith("!")) {
						newSegment = segment[1..];
						state = false;
					}
					if (newSegment.StartsWith("b;")) {
						AddComponent(new MessageComponent(newSegment[2..], bold: state));
						continue;
					}
					if (newSegment.StartsWith("i;")) {
						AddComponent(new MessageComponent(newSegment[2..], italics: state));
						continue;
					}
					if (newSegment.StartsWith("u;")) {
						AddComponent(new MessageComponent(newSegment[2..], underline: state));
						continue;
					}
					if (newSegment.StartsWith("s;")) {
						AddComponent(new MessageComponent(newSegment[2..], strike: state));
						continue;
					}
				}

				// If the loop makes it here, no format was followed. Just write it verbatim.
				AddComponent(new MessageComponent(segment));
			}
		}

		/// <summary>
		/// Constructs a <see cref="LogMessage"/> from the provided components in order.
		/// </summary>
		/// <param name="components"></param>
		public LogMessage(params MessageComponent[] components) {
			foreach (MessageComponent cmp in components) {
				AddComponent(cmp);
			}
		}

		/// <summary>
		/// Takes in the given <see cref="string"/> verbatim and does not process any color codes. The returned <see cref="LogMessage"/> will have a single <see cref="MessageComponent"/> storing the raw string.
		/// </summary>
		/// <param name="raw"></param>
		/// <returns></returns>
		public static LogMessage WithoutFormatting(string raw) {
			return new LogMessage(new MessageComponent(raw));
		}

		#endregion

		#region Component Registry

		/// <summary>
		/// Removes the given <see cref="MessageComponent"/> from the log message if it's present.
		/// </summary>
		/// <param name="component"></param>
		public void RemoveComponent(MessageComponent component) {
			ComponentsInternal.Remove(component);
		}

		/// <summary>
		/// Appends the given <see cref="MessageComponent"/> on to the end of this log message.
		/// </summary>
		/// <param name="component"></param>
		public void AddComponent(MessageComponent component) {
			ComponentsInternal.Add(component);
		}

		/// <summary>
		/// Adds the given parameters by constructing a new <see cref="MessageComponent"/> with the given args.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="color"></param>
		/// <param name="background"></param>
		/// <param name="bold"></param>
		/// <param name="underline"></param>
		/// <param name="italics"></param>
		/// <param name="strike"></param>
		public void AddComponent(string text, Color? color, Color? background = null, bool? bold = null, bool? underline = null, bool? italics = null, bool? strike = null) {
			AddComponent(new MessageComponent(text, color, background, bold, underline, italics, strike));
		}

		/// <summary>
		/// Appends the given <see cref="MessageComponent"/>s in order on to the end of this log message.
		/// </summary>
		/// <param name="components"></param>
		public void AddComponents(params MessageComponent[] components) {
			foreach (MessageComponent cmp in components) {
				ComponentsInternal.Add(cmp);
			}
		}

		/// <summary>
		/// Appends this string to the end of the <see cref="LogMessage"/>, processing all color codes inside.
		/// </summary>
		/// <param name="formattedString"></param>
		public void AddComponent(string formattedString) {
			AddComponents(new LogMessage(formattedString).Components.ToArray());
		}

		/// <summary>
		/// Appends this string to the end of the <see cref="LogMessage"/> without processing any color codes.
		/// </summary>
		/// <param name="rawString"></param>
		public void AddRaw(string rawString) {
			ComponentsInternal.Add(new MessageComponent(rawString));
		}

		/// <summary>
		/// Adds all <see cref="MessageComponent"/>s from <paramref name="others"/> in order to the end of this <see cref="LogMessage"/>.<para/>
		/// Returns a reference to <see langword="this"/>.
		/// </summary>
		/// <param name="others"></param>
		public LogMessage ConcatLocal(params LogMessage[] others) {
			foreach (LogMessage msg in others) {
				AddComponents(msg.Components.ToArray());
			}
			return this;
		}

		/// <summary>
		/// Constructs a new <see cref="LogMessage"/> containing all <see cref="MessageComponent"/>s from <paramref name="originalMessage"/> followed by all <see cref="MessageComponent"/>s from <paramref name="others"/> in order.
		/// </summary>
		/// <param name="originalMessage"></param>
		/// <param name="others"></param>
		/// <returns></returns>
		public static LogMessage Concat(LogMessage originalMessage, params LogMessage[] others) {
			List<MessageComponent> msgs = originalMessage.Components.ToList();
			foreach (LogMessage msg in others) {
				msgs.AddRange(msg.Components);
			}
			return new LogMessage(msgs.ToArray());
		}

		#endregion

		#region Conversion

		/// <summary>
		/// Returns the given string with VT sequences installed for the component information.<para/>
		/// Of course, this is only usable if whatever the message is being written to supports VT sequences.
		/// </summary>
		/// <returns></returns>
		public string ToVTString() {
			StringBuilder resultString = new StringBuilder();
			foreach (MessageComponent component in ComponentsInternal) {
				resultString.Append(component.ToVTString());
			}
			return resultString.ToString();
		}

		/// <summary>
		/// Takes a string generated by <see cref="ToVTString"/> and translates it back into a <see cref="LogMessage"/>.<para/>
		/// This is designed for use by external libraries, e.g. if a logging GUI implements <see cref="OutputRelay"/>, it can use this to get raw color objects instead of VT codes.
		/// </summary>
		/// <param name="vtString">The string that presumably contains VT sequences. All unknown sequences will be written verbatim.</param>
		/// <param name="defaultTextColor">The default text color. This color is applied if a reset code is detected. If it is not specified, white will be used.</param>
		/// <param name="defaultBackgroundColor">The default background color. This color is applied if a reset code is detected. If it is not specified, black will be used.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"/>
		public static LogMessage FromVTFormattedString(string vtString, Color? defaultTextColor = null, Color? defaultBackgroundColor = null) {
			if (vtString == null) throw new ArgumentNullException(nameof(vtString));
			string[] splitByEscape = vtString.Split('\u001b');
			if (splitByEscape.Length == 1) return new LogMessage(vtString);

			Color defText = defaultTextColor.GetValueOrDefault(Color.WHITE);
			Color defBg = defaultBackgroundColor.GetValueOrDefault(Color.BLACK);


			LogMessage result = new LogMessage();
			foreach (string component in splitByEscape) {
				if (!component.StartsWith("[")) {
					result.AddComponent(new MessageComponent('\u001b' + component));
					continue;
				}
				string code = GetStringBetween(component, '[', 'm');
				if (code == null) {
					result.AddComponent(new MessageComponent('\u001b' + component));
					continue;
				}

				Color? foreColor = null;
				Color? backColor = null;
				bool? underline = null;

				if (int.TryParse(code, out int value)) {
					if (value == 4) {
						// 4 = Adds underline
						underline = true;
					} else if (value == 24) {
						// 24 = Removes underline
						underline = false;
					} else if (value == 0) {
						// 0 = Returns all attributes to the default state prior to modification
						foreColor = defText;
						backColor = defBg;
						underline = false;
					}
				} else {
					// This probably is split by semicolons.
					string[] codeValues = code.Split(';');
					byte[] numValues = new byte[5];
					// "a;b;c;d;e";
					if (codeValues.Length != 5) {
						result.AddComponent(new MessageComponent('\u001b' + component));
						continue;
					}
					// 38/48
					// 2
					// r;g;b

					bool fail = false;
					for (int idx = 0; idx < 5; idx++) {
						if (byte.TryParse(codeValues[idx], out byte v)) {
							numValues[idx] = v;
						} else {
							fail = true;
							break;
						}
					}
					if (fail || numValues[1] != 2 || (numValues[0] != 38 && numValues[0] != 48)) {
						result.AddComponent(new MessageComponent('\u001b' + component));
						continue;
					}

					if (numValues[0] == 38) {
						foreColor = new Color(numValues[2], numValues[3], numValues[4]);
					} else if (numValues[0] == 48) {
						backColor = new Color(numValues[2], numValues[3], numValues[4]);
					}
				}

				result.AddComponent(new MessageComponent(component[(component.IndexOf("m") + 1)..], foreColor, backColor, underline));
			}
			return result;
		}

		#endregion

		#region Object Overrides

		/// <summary>
		/// Returns the raw, plain-text included in this <see cref="LogMessage"/> without any formatting codes.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			StringBuilder resultString = new StringBuilder();
			foreach (MessageComponent component in ComponentsInternal) {
				resultString.Append(component.Text);
			}
			return resultString.ToString();
		}

		#endregion

		#region Utilities

		/// <summary>
		/// Given the text, this method will find the first occurrence of <paramref name="start"/> and <paramref name="end"/>.<para/>
		/// If both exist and end is after start, it will return all characters between these two (NOT including them)<para/>
		/// If this case is not met, <see langword="null"/> is returned.
		/// </summary>
		/// <param name="text">The string to search.</param>
		/// <param name="start">The character that marks the start of the result.</param>
		/// <param name="end">The character that marks the end of the result.</param>
		/// <param name="after">Starts the search after this character index.</param>
		/// <returns></returns>
		public static string GetStringBetween(string text, char start, char end, int after = 0) {
			if (!text.Contains(start) || !text.Contains(end)) return null;
			int startIdx = text.IndexOf(start, after);
			if (startIdx == -1) return null;
			int endIdx = text.IndexOf(end, Math.Max(after, startIdx));
			if (endIdx == -1) return null;

			int length = endIdx - startIdx;
			return text.Substring(startIdx + 1, length - 1);
		}

		#endregion

		/// <summary>
		/// Represents a colored component of a message.
		/// </summary>
		public sealed class MessageComponent {

			#region Properties

			/// <summary>
			/// The raw content of this component.
			/// </summary>
			public string Text { get; set; }


			/// <summary>
			/// The color of the text in this component, or <see langword="null"/> to make no change to the existing state in the log.
			/// </summary>
			public Color? Color { get; set; }

			/// <summary>
			/// The color of the background in this component, or <see langword="null"/> to make no change to the existing state in the log.
			/// </summary>
			public Color? BackgroundColor { get; set; }

			/// <summary>
			/// Whether or not to make this message bold, or <see langword="null"/> to make no change to the existing state in the log.
			/// </summary>
			public bool? Bold { get; set; }

			/// <summary>
			/// Whether or not to make this message italicized, or <see langword="null"/> to make no change to the existing state in the log.
			/// </summary>
			public bool? Italics { get; set; }

			/// <summary>
			/// Whether or not to underline this message, or <see langword="null"/> to make no change to the existing state in the log.
			/// </summary>
			public bool? Underline { get; set; }

			/// <summary>
			/// Whether or not to make this message strikethrough, or <see langword="null"/> to make no change to the existing state in the log.
			/// </summary>
			public bool? Strike { get; set; }

			#endregion

			#region Constructors

			/// <summary>
			/// Construct a new <see cref="MessageComponent"/> from the given text and color that can optionally be bold and/or underlined.<para/>
			/// This does not process custom color codes in the text.
			/// </summary>
			/// <param name="text">The raw text.</param>
			/// <param name="color">The color of the text.</param>
			/// <param name="backgroundColor">The color of the background for this text, good for highlight.</param>
			/// <param name="bold">Whether or not to bold this text.</param>
			/// <param name="underline">Whether or not to underline this text.</param>
			/// <param name="italics">Whether or not to use italics for this text.</param>
			/// <param name="strike">Whether or not to strikethrough this text.</param>
			public MessageComponent(string text, Color? color = null, Color? backgroundColor = null, bool? bold = null, bool? underline = null, bool? italics = null, bool? strike = null) {
				if (text.Contains("\u001b")) throw new ArgumentException("MessageComponent contained a VT sequence!");
				Text = text;
				Color = color;
				BackgroundColor = backgroundColor;
				Bold = bold;
				Underline = underline;
				Italics = italics;
				Strike = strike;
			}

			#endregion

			#region Conversion

			/// <summary>
			/// Returns the raw VT sequence that can recreate the information stored in this <see cref="MessageComponent"/> in a console that supports VT.<para/>
			/// Only properties that have non-<see langword="null"/> values will be applied.
			/// </summary>
			/// <returns></returns>
			public string ToVTString() {
				StringBuilder result = new StringBuilder();
				if (Underline.HasValue) {
					if (Underline.Value) {
						result.Append(Data.Structs.Color.VT_SEQ_UNDERLINE);
					} else {
						result.Append(Data.Structs.Color.VT_SEQ_REMOVE_UNDERLINE);
					}
				}
				if (Color.HasValue) result.Append(string.Format(Data.Structs.Color.VT_SEQ_COLOR, Data.Structs.Color.VT_COLOR_FOREGROUND, Color.Value.R, Color.Value.G, Color.Value.B));
				if (BackgroundColor.HasValue) result.Append(string.Format(Data.Structs.Color.VT_SEQ_COLOR, Data.Structs.Color.VT_COLOR_BACKGROUND, BackgroundColor.Value.R, BackgroundColor.Value.G, BackgroundColor.Value.B));
				result.Append(Text);
				return result.ToString();
			}

			#endregion

			#region Object Overrides

			//private const string TOSTRING_FORMAT = "[{4}Color=[{5}{0}{4}] BackgroundColor=[{5}{1}{4}] IsUnderlined={5}{2}{4} Text='{5}{3}{4}']";
			
			//private const string TOSTRING_FORMAT_NL = "[\n\t{4}Color=[{5}{0}{4}]\n\tBackgroundColor=[{5}{1}{4}]\n\tIsUnderlined={5}{2}{4}\n\tText='{5}{3}{4}'\n]";

			private const string INHERITED_TAG = "<NO CHANGE>";

			/// <summary>
			/// Returns a dump of this <see cref="MessageComponent"/>'s data as a single-line, uncolored piece of text.
			/// </summary>
			/// <inheritdoc cref="ToString(bool, bool)"/>
			public override string ToString() => ToString(false, true);

			/// <summary>
			/// Returns a dump of this <see cref="MessageComponent"/>'s data, optionally adding newlines between elements of the string and coloring the output text..
			/// </summary>
			/// <remarks>
			/// This is for debugging purposes. To acquire a <see cref="string"/> for use in logs, consider using <see cref="Text"/> (for plain text) or <see cref="ToVTString"/> (for rich text).
			/// </remarks>
			/// <param name="addNewlines">Whether or not to use a larger (but easier to read) variant of this string that adds lines between each property.</param>
			/// <param name="withoutColoring">If <see langword="true"/>, no colors will be put into this message.</param>
			/// <returns></returns>
			public string ToString(bool addNewlines, bool withoutColoring = false) {
				if (!addNewlines) return GetStockTostring(withoutColoring); //return string.Format(TOSTRING_FORMAT, color, bgColor, under, Text, preClr, postClr);
																			//return string.Format(TOSTRING_FORMAT_NL, color, bgColor, under, Text, preClr, postClr);
				return GetNLToString(withoutColoring);
			}

			private string GetStockTostring(bool withoutColoring) {
				string color = Color?.ToString() ?? INHERITED_TAG;
				string bgColor = BackgroundColor?.ToString() ?? INHERITED_TAG;
				string bold = Bold?.ToString() ?? INHERITED_TAG;
				string italics = Italics?.ToString() ?? INHERITED_TAG;
				string under = Underline?.ToString() ?? INHERITED_TAG;
				string strike = Strike?.ToString() ?? INHERITED_TAG;
				string preClr = withoutColoring ? "" : Data.Structs.Color.GetVTForColor(new Color(127, 127, 127));
				string postClr = withoutColoring ? "" : Data.Structs.Color.GetVTForColor(new Color(192, 63, 63));

				return $"[{preClr}Color=[{preClr}{color}{postClr}] BackgroundColor=[{preClr}{bgColor}{postClr}] IsBold=[{preClr}{bold}{postClr}] IsItalics=[{preClr}{italics}{postClr}] IsUnderline=[{preClr}{under}{postClr}] IsStrike=[{preClr}{strike}{postClr}] Text={preClr}{Text}{postClr}]";
			}

			private string GetNLToString(bool withoutColoring) {

				string color = Color?.ToString() ?? INHERITED_TAG;
				string bgColor = BackgroundColor?.ToString() ?? INHERITED_TAG;
				string bold = Bold?.ToString() ?? INHERITED_TAG;
				string italics = Italics?.ToString() ?? INHERITED_TAG;
				string under = Underline?.ToString() ?? INHERITED_TAG;
				string strike = Strike?.ToString() ?? INHERITED_TAG;
				string preClr = withoutColoring ? "" : Data.Structs.Color.GetVTForColor(new Color(127, 127, 127));
				string postClr = withoutColoring ? "" : Data.Structs.Color.GetVTForColor(new Color(192, 63, 63));

				return $"[\n\t{preClr}Color=[{preClr}{color}{postClr}]\n\tBackgroundColor=[{preClr}{bgColor}{postClr}]\n\tIsBold=[{preClr}{bold}{postClr}]\n\tIsItalics=[{preClr}{italics}{postClr}]\n\tIsUnderline=[{preClr}{under}{postClr}]\n\tIsStrike=[{preClr}{strike}{postClr}]\n\tText={preClr}{Text}{postClr}\n]";
			}

			#endregion

		}
	}
}

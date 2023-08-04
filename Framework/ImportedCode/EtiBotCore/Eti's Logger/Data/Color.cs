using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiLogger.Data.Util;

namespace EtiLogger.Data.Structs {

	/// <summary>
	/// An RGB color.
	/// </summary>
	public readonly struct Color : IEquatable<Color> {

		#region VT Constants

		/// <summary>
		/// A component for use in <c>VT_SEQ_COLOR {0}</c> that causes the given color to be applied to the foreground.
		/// </summary>
		public const string VT_COLOR_FOREGROUND = "38;2";

		/// <summary>
		/// A component for use in <c>VT_SEQ_COLOR {0}</c> that causes the given color to be applied to the background.
		/// </summary>
		public const string VT_COLOR_BACKGROUND = "48;2";

		/// <summary>
		/// A format string for a VT sequence that changes the color. Format params are:
		/// <list type="table">
		/// <item>
		/// <term><c>{0}</c></term>
		/// <description>Either <see cref="VT_COLOR_FOREGROUND"/> or <see cref="VT_COLOR_BACKGROUND"/></description>
		/// </item>
		/// <item>
		/// <term><c>{1}</c></term>
		/// <description>The red component of the color from 0 to 255.</description>
		/// </item>
		/// <item>
		/// <term><c>{2}</c></term>
		/// <description>The green component of the color from 0 to 255.</description>
		/// </item>
		/// <item>
		/// <term><c>{3}</c></term>
		/// <description>The blue component of the color from 0 to 255.</description>
		/// </item>
		/// </list>
		/// </summary>
		public const string VT_SEQ_COLOR = "\u001b[{0};{1};{2};{3}m";

		/// <summary>
		/// A VT sequence that causes all following text to be underlined.
		/// </summary>
		public const string VT_SEQ_UNDERLINE = "\u001b[4m";

		/// <summary>
		/// A VT sequence that causes all following text to not be underlined (cancels <see cref="VT_SEQ_UNDERLINE"/>)
		/// </summary>
		public const string VT_SEQ_REMOVE_UNDERLINE = "\u001b[24m";

		/// <summary>
		/// A VT sequence that resets all color and underline values to their defaults.
		/// </summary>
		public const string VT_RESET = "\u001b[0m";

		#endregion

		#region Preset Colors

		#region Grayscale

		/// <summary>0, 0, 0</summary>
		public static readonly Color BLACK = new Color(0, 0, 0);

		/// <summary>31, 31, 31</summary>
		public static readonly Color DARKEST_GRAY = new Color(31, 31, 31);

		/// <summary>63, 63, 63</summary>
		public static readonly Color DARKER_GRAY = new Color(63, 63, 63);

		/// <summary>127, 127, 127</summary>
		public static readonly Color GRAY = new Color(127, 127, 127);

		/// <summary>192, 192, 192</summary>
		public static readonly Color LIGHTER_GRAY = new Color(192, 192, 192);

		/// <summary>224, 224, 224</summary>
		public static readonly Color LIGHTEST_GRAY = new Color(224, 224, 224);

		/// <summary>255, 255, 255</summary>
		public static readonly Color WHITE = new Color(255, 255, 255);

		#endregion

		#region Hues

		#region Bright
		/// <summary>255, 0, 0</summary>
		public static readonly Color RED = new Color(255, 0, 0);

		/// <summary>255, 63, 0</summary>
		public static readonly Color RED_ORANGE = new Color(255, 63, 0);

		/// <summary>255, 127, 0</summary>
		public static readonly Color ORANGE = new Color(255, 127, 0);

		/// <summary>255, 255, 0</summary>
		public static readonly Color YELLOW = new Color(255, 255, 0);

		/// <summary>127, 255, 0</summary>
		public static readonly Color YELLOW_GREEN = new Color(127, 255, 0);

		/// <summary>0, 255, 0</summary>
		public static readonly Color GREEN = new Color(0, 255, 0);

		/// <summary>0, 255, 127</summary>
		public static readonly Color GREEN_CYAN = new Color(0, 255, 127);

		/// <summary>0, 255, 255</summary>
		public static readonly Color CYAN = new Color(0, 255, 255);

		/// <summary>0, 127, 255</summary>
		public static readonly Color CYAN_BLUE = new Color(0, 127, 255);

		/// <summary>0, 0, 255</summary>
		public static readonly Color BLUE = new Color(0, 0, 255);

		/// <summary>255, 0, 255</summary>
		public static readonly Color MAGENTA = new Color(255, 0, 255);
		#endregion

		#region Dark
		/// <summary>127, 0, 0</summary>
		public static readonly Color DARK_RED = new Color(127, 0, 0);

		/// <summary>127, 31, 0</summary>
		public static readonly Color DARK_RED_ORANGE = new Color(127, 31, 0);

		/// <summary>127, 63, 0</summary>
		public static readonly Color DARK_ORANGE = new Color(127, 63, 0);

		/// <summary>127, 127, 0</summary>
		public static readonly Color DARK_YELLOW = new Color(127, 127, 0);

		/// <summary>63, 127, 0</summary>
		public static readonly Color DARK_YELLOW_GREEN = new Color(63, 127, 0);

		/// <summary>0, 127, 0</summary>
		public static readonly Color DARK_GREEN = new Color(0, 127, 0);

		/// <summary>0, 127, 63</summary>
		public static readonly Color DARK_GREEN_CYAN = new Color(0, 127, 63);

		/// <summary>0, 127, 127</summary>
		public static readonly Color DARK_CYAN = new Color(0, 127, 127);

		/// <summary>0, 63, 127</summary>
		public static readonly Color DARK_CYAN_BLUE = new Color(0, 63, 127);

		/// <summary>0, 0, 127</summary>
		public static readonly Color DARK_BLUE = new Color(0, 0, 127);

		/// <summary>127, 0, 127</summary>
		public static readonly Color DARK_MAGENTA = new Color(127, 0, 127);

		#endregion

		#endregion

		#region Stragglers

		/// <summary>255, 183, 66</summary>
		public static readonly Color GOLD = new Color(255, 183, 66);

		/// <summary>15, 0, 0</summary>
		public static readonly Color BLOOD_RED = new Color(15, 0, 0);

		/// <summary>217, 252, 255</summary>
		public static readonly Color SPIRIT_BLUE = new Color(217, 252, 255);

		#endregion

		#endregion

		#region Main Fields

		/// <summary>
		/// The red component of this color.
		/// </summary>
		public readonly byte R;

		/// <summary>
		/// The green component of this color.
		/// </summary>
		public readonly byte G;

		/// <summary>
		/// The blue component of this color.
		/// </summary>
		public readonly byte B;

		/// <summary>
		/// The color condensed into the bytes <c>0RGB</c>
		/// </summary>
		public readonly int Value;

		/// <summary>
		/// A hexadecimal representation of this color, e.g. <c>FFFFFF</c> for <c>(255, 255, 255)</c>
		/// </summary>
		public string Hex => Value.ToString("X6").ToUpper();

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs a new <see cref="Color"/> from the given red, green, and blue components.
		/// </summary>
		/// <param name="r">The red component of the color.</param>
		/// <param name="g">The green component of the color.</param>
		/// <param name="b">The blue component of the color.</param>
		public Color(byte r, byte g, byte b) {
			R = r;
			G = g;
			B = b;
			Value = (r << 16) | (g << 8) | b;
		}

		/// <summary>
		/// Constructs a new <see cref="Color"/> from the given condensed integer color, which expects the bytes to be <c>0RGB</c>.
		/// </summary>
		/// <param name="color">An integer value storing an RGB color, e.g. <c>0xFFFFFF</c></param>
		public Color(int color) {
			color &= 0x00FFFFFF;
			Value = color;
			R = (byte)((color >> 16) & (0xFF));
			G = (byte)((color >> 8) & (0xFF));
			B = (byte)(color & (0xFF));
		}

		/// <summary>
		/// Constructs a new <see cref="Color"/> from the given hexadecimal value which should be <strong>exactly</strong> 6 characters long (e.g. <c>FFFFFF</c>).
		/// </summary>
		/// <param name="hexColor">A string that is expected to be 6 characters long representing a hex color code, e.g. <c>FFFFFF</c></param>
		/// <exception cref="ArgumentException">If the hex string is not 6 characters long, or if <see cref="Convert.ToInt32(string, int)"/> throws an exception of this type.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Propagated from <see cref="Convert.ToInt32(string, int)"/>.</exception>
		/// <exception cref="FormatException">Propagated from <see cref="Convert.ToInt32(string, int)"/>.</exception>
		/// <exception cref="OverflowException">Propagated from <see cref="Convert.ToInt32(string, int)"/>.</exception>
		public Color(string hexColor) {
			if (hexColor.Length != 6) throw new ArgumentException("Color string is not 6 characters long!", nameof(hexColor));
			int color = Convert.ToInt32(hexColor, 16);
			R = (byte)((color >> 16) & (0xFF));
			G = (byte)((color >> 8) & (0xFF));
			B = (byte)(color & (0xFF));
			Value = color;
		}

		#endregion

		#region Static Methods

		/// <summary>
		/// A quick way of getting the VT sequence string for the given color.
		/// </summary>
		/// <param name="color">The desired color.</param>
		/// <param name="background">If <see langword="true"/>, then this color will apply to the background instead of the foreground.</param>
		/// <returns></returns>
		public static string GetVTForColor(Color color, bool background = false) {
			return string.Format(VT_SEQ_COLOR, background ? VT_COLOR_BACKGROUND : VT_COLOR_FOREGROUND, color.R, color.G, color.B);
		}

		/// <summary>
		/// "Colors" this string by adding a VT sequence for the given color before it that affects the text.
		/// </summary>
		/// <param name="text">The text to edit.</param>
		/// <param name="color">The color to apply to the text.</param>		
		/// <param name="withReset">If <see langword="true"/>, then the reset code will be placed at the end of this string, which will revert all changes to the default values.</param>
		/// <returns></returns>
		public static string ColorString(string text, Color color, bool withReset = true) {
			string retn = GetVTForColor(color, false) + text;
			if (withReset) retn += VT_RESET;
			return retn;
		}

		/// <summary>
		/// "Highlights" this string by adding a VT sequence for the given color before it that affects the background.
		/// </summary>
		/// <param name="text">The text to edit.</param>
		/// <param name="color">The color to apply behind the text.</param>
		/// <param name="withReset">If <see langword="true"/>, then the reset code will be placed at the end of this string, which will revert all changes to the default values.</param>
		/// <returns></returns>
		public static string HighlightString(string text, Color color, bool withReset = true) {
			string retn = GetVTForColor(color, true) + text;
			if (withReset) retn += VT_RESET;
			return retn;
		}

		/// <summary>
		/// Appends the VT Underline start code before this text and the VT Underline end code after this text.
		/// </summary>
		/// <param name="text">The text to underline.</param>
		/// <returns></returns>
		public static string UnderlineString(string text) {
			return VT_SEQ_UNDERLINE + text + VT_SEQ_REMOVE_UNDERLINE;
		}

		/// <summary>
		/// Applies color, background color, and underline to the given string. Using <see langword="null"/> values will cause that property to remain unchanged.
		/// </summary>
		/// <param name="text">The text to change.</param>
		/// <param name="color">The desired color of the text, or <see langword="null"/> to keep the existing color.</param>
		/// <param name="underline">Whether to enable or disable underline explicitly, or <see langword="null"/> to keep the existing state.</param>
		/// <param name="backgroundColor">The desired background or highlight color of the text, or <see langword="null"/> to keep the existing color.</param>
		/// <param name="restoreColor">The color made before this call, used to ensure that it only affects this text. If set, this will be applied at the end of the string. If unset, nothing will be applied.</param>
		/// <param name="restoreBackgroundColor">The original background color before this call, used to ensure that it only affects this text. If set, this will be applied at the end of the string. If unset, nothing will be applied.</param>
		/// <param name="withReset">If <see langword="true"/>, then the reset code will be placed at the end of this string, which will revert all changes to the default values. Additionally, <paramref name="restoreColor"/> and <paramref name="restoreBackgroundColor"/> will be ignored.</param>
		/// <returns></returns>
		public static string FormatString(string text, Color? color = null, bool? underline = null, Color? backgroundColor = null, Color? restoreColor = null, Color? restoreBackgroundColor = null, bool withReset = false) {
			if (color != null) {
				text = ColorString(text, color.Value, withReset);
			}
			if (backgroundColor != null) {
				text = HighlightString(text, backgroundColor.Value, withReset);
			}
			if (underline != null) {
				if (underline.Value) {
					text = UnderlineString(text);
				} else {
					text += VT_SEQ_REMOVE_UNDERLINE; // Only disable.
				}
			}
			if (withReset) {
				text += VT_RESET;
			} else {
				if (restoreColor != null) {
					text += GetVTForColor(restoreColor.Value, false);
				}
				if (restoreBackgroundColor != null) {
					text += GetVTForColor(restoreBackgroundColor.Value, true);
				}
			}
			return text;
		}

		/// <summary>
		/// Returns a string that will apply a color, background color, and underline to all text after it. Using <see langword="null"/> values will cause that property to remain unchanged.<para/>
		/// Equivalent to calling <see cref="FormatString(string, Color?, bool?, Color?, Color?, Color?, bool)"/> with an empty <see cref="string"/> for the first parameter.
		/// </summary>
		/// <param name="color">The desired color of the text, or <see langword="null"/> to keep the existing color.</param>
		/// <param name="underline">Whether to enable or disable underline explicitly, or <see langword="null"/> to keep the existing state.</param>
		/// <param name="backgroundColor">The desired background or highlight color of the text, or <see langword="null"/> to keep the existing color.</param>
		/// <param name="restoreColor">The color made before this call, used to ensure that it only affects this text. If set, this will be applied at the end of the string. If unset, nothing will be applied.</param>
		/// <param name="restoreBackgroundColor">The original background color before this call, used to ensure that it only affects this text. If set, this will be applied at the end of the string. If unset, nothing will be applied.</param>
		/// <returns></returns>
		public static string GetFormatString(Color? color = null, bool? underline = null, Color? backgroundColor = null, Color? restoreColor = null, Color? restoreBackgroundColor = null) {
			return FormatString(string.Empty, color, underline, backgroundColor, restoreColor, restoreBackgroundColor, false);
		}

		#endregion

		#region Instance Methods

		/// <summary>
		/// A display trick that will linearly interpolate this <see cref="Color"/> to the specified <paramref name="backgroundColor"/> based on <paramref name="opacity"/>, where <c>opacity=0</c> will return a color equal to the <paramref name="backgroundColor"/> and where <c>opacity=255</c> will return a color equal to this color.
		/// </summary>
		/// <param name="backgroundColor">The color of the background.</param>
		/// <param name="opacity">The opacity of this color, where 0 means it is completely invisible against the background and 255 means it is completely visible against the background.</param>
		/// <returns></returns>
		public Color WithPseudoOpacity(Color backgroundColor, byte opacity) {
			byte r = Util.Math.LerpByte(backgroundColor.R, R, opacity / 255f);
			byte g = Util.Math.LerpByte(backgroundColor.G, G, opacity / 255f);
			byte b = Util.Math.LerpByte(backgroundColor.B, B, opacity / 255f);
			return new Color(r, g, b);
		}

		/// <summary>
		/// Returns this <see cref="Color"/> as a <see cref="System.Drawing.Color"/> instance.
		/// </summary>
		/// <returns></returns>
		public System.Drawing.Color ToSystemColor() => System.Drawing.Color.FromArgb(R, G, B);
		

		#endregion

		#region Object Overrides

		/// <inheritdoc/>
		public static bool operator ==(Color left, Color right) {
			return left.Value == right.Value;
		}

		/// <inheritdoc/>
		public static bool operator !=(Color left, Color right) {
			return left.Value != right.Value;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if (ReferenceEquals(this, obj)) return true;
			return obj is Color color && Value == color.Value;
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return Value;
		}

		/// <summary>
		/// Identical to referencing <see cref="Hex"/>.
		/// </summary>
		/// <returns></returns>
		public override string ToString() => Hex;

		/// <inheritdoc/>
		public bool Equals(Color other) {
			return Value == other.Value;
		}

		#endregion

	}
}

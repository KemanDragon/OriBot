using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Payloads.Data;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;

namespace OldOriBot.Data.Flair {
	public class UserColor {

		public const double MOD_DESAT = 0.66;
		public const double MOD_VALUE = 1;

		[Obsolete] public const int BRIGHT = 0xFFFFFF;
		public const int MID = 0xA8A8A8;
		public const int DARK = 0x545454;
		[Obsolete] public const int CARBON = 0x111111;

		public const int RED = 0xFF0000;
		public const int ORANGE = 0xFF8000;
		public const int YELLOW = 0xFFFF00;
		public const int GRASS = 0x80FF00;
		public const int LIME = 0x00FF00;
		[Obsolete] public const int MAGENTA = 0xFF0080;
		[Obsolete] public const int PINK = 0xFF00FF;
		public const int VIOLET = 0xC000FF;
		public const int CYAN = 0x00BCB6;
		public const int BLUE = 0x003FFF;
		public const int GRAY = 0xA8A8A8;

		/// <summary>
		/// A binding of string keywords to their color-value counterpart.
		/// </summary>
		public static Dictionary<string, int> ColorKeywords => new Dictionary<string, int> {
			["red"] = RED,
			["orange"] = ORANGE,
			["yellow"] = YELLOW,
			["grass"] = GRASS,
			["green"] = LIME,
			["violet"] = VIOLET,
			["gray"] = GRAY,
			["cyan"] = CYAN,
			["blue"] = BLUE,

			["white"] = 0xFFFFFF,
		};


		/// <summary>
		/// A binding of string keywords to their modifier-value counterpart.
		/// </summary>
		public static Dictionary<string, int> ModifierKeywords => new Dictionary<string, int> {
			//["bright"] = BRIGHT,
			["mid"] = MID,
			["dark"] = DARK
		};

		public static Dictionary<string, Role> NameCache = new Dictionary<string, Role>() { };

		public int Value { get; }

		public static void ClearCache() {
			NameCache.Clear();
		}

		private static Color ColorFromInt(int value) {
			return Color.FromArgb((value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF);
		}

		private static int IntFromColor(Color c) {
			return (c.R << 16) | (c.G << 8) | c.B;
		}

		private UserColor(int color) {
			Color c = ColorFromInt(color);
			ColorToHSV(c, out double hue, out double sat, out double val);
			Value = IntFromColor(ColorFromHSV(hue, sat * MOD_DESAT, val * MOD_VALUE)); ;
			//Value = color;
		}

		/// <summary>
		/// Generate a <see cref="UserColor"/> from color/modifier keywords.
		/// </summary>
		/// <param name="keywords"></param>
		/// <returns></returns>
		public static UserColor FromKeywords(string keyword0, string keyword1) {
			if (keyword0 == "grey") keyword0 = "gray";
			if (keyword1 == "grey") keyword1 = "gray";
			if (keyword0 == "purple") keyword0 = "violet";
			if (keyword1 == "purple") keyword1 = "violet";
			if (keyword0 == "teal") keyword0 = "cyan";
			if (keyword1 == "teal") keyword1 = "cyan";

			int value;
			bool usedColor = false;
			bool usedModifier = false;
			if (ColorKeywords.TryGetValue(keyword0.ToLower(), out int color)) {
				usedColor = true;
				value = color;
			} else if (ModifierKeywords.TryGetValue(keyword0.ToLower(), out int modifier)) {
				usedModifier = true;
				value = modifier;
			} else {
				throw new ArgumentException($"The first specified keyword does not have an associated binding (invalid keyword `{keyword0.ToLower()}`). Use `>> help colorme` for a list of keywords.", new NoThrowDummyException());
			}

			// Use logical AND on these.
			if (ColorKeywords.TryGetValue(keyword1.ToLower(), out color)) {
				if (usedColor) {
					throw new ArgumentException("A second color keyword cannot be specified. Please specify a color and a modifier, not two colors.", new NoThrowDummyException());
				}
				value &= color;
			} else if (ModifierKeywords.TryGetValue(keyword1.ToLower(), out int modifier)) {
				if (usedModifier) {
					throw new ArgumentException("A second modifier keyword cannot be specified. Please specify a color and a modifier, not two modifiers.", new NoThrowDummyException());
				}
				value &= modifier;
			} else {
				throw new ArgumentException($"The second specified keyword does not have an associated binding (invalid keyword `{keyword1.ToLower()}`). Use `>> help colorme` for a list of keywords.", new NoThrowDummyException());
			}

			return new UserColor(value);
		}

		/// <summary>
		/// Instantiates all colors.
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		public static async Task InstantiateAllColors(BotContext ctx) {
			foreach (string keyword1 in ModifierKeywords.Keys) {
				foreach (string keyword0 in ColorKeywords.Keys) {
					await GetRoleFromColor(ctx, keyword0, keyword1); // does await Task.Delay(500); if it had to create a new role
				}
			}
		}

		public static async Task<Role> GetRoleFromColor(BotContext ctx, string keyword0, string keyword1) {
			keyword0 = (keyword0 ?? "").ToLower();
			keyword1 = (keyword1 ?? "").ToLower();
			if (keyword0 == "grey") keyword0 = "gray";
			if (keyword1 == "grey") keyword1 = "gray";
			if (keyword0 == "purple") keyword0 = "violet";
			if (keyword1 == "purple") keyword1 = "violet";
			if (keyword0 == "teal") keyword0 = "cyan";
			if (keyword1 == "teal") keyword1 = "cyan";

			if (keyword0 == "white") keyword1 = null;
			if (keyword1 == "white") keyword0 = null;
			if (string.IsNullOrWhiteSpace(keyword0)) keyword0 = null;
			if (string.IsNullOrWhiteSpace(keyword1)) keyword1 = null;

			string kName = NameFromKeywords(keyword0, keyword1);
			string name = "UserColor [" + kName + "]"; // No need to sanity check here since ^ does that.
			if (NameCache.TryGetValue(name, out Role r)) return r;

			foreach (Role existingRole in ctx.Server.Roles) {
				if (existingRole.Name == name) {
					NameCache[name] = existingRole;
					return existingRole;
				}
			}
			if (keyword0 != null && keyword1 != null) {
				UserColor color = FromKeywords(keyword0, keyword1);
				//DiscordRole newRole = await ctx.Server.CreateRoleAsync(name, color: new DiscordColor(color.Value));
				Role newRole = await ctx.Server.CreateNewRoleAsync(name, Permissions.None, color.Value, false, false, "Server was missing this color role.");
				NameCache[name] = newRole;
				await Task.Delay(500);
				return newRole;
			} else {
				// White was used.
				UserColor color = new UserColor(ColorKeywords["white"]);
				Role newRole = await ctx.Server.CreateNewRoleAsync(name, Permissions.None, color.Value, false, false, "Server was missing this color role.");
				NameCache[name] = newRole;
				await Task.Delay(500);
				return newRole;
			}
		}

		private static string NameFromKeywords(string keyword0, string keyword1) {
			// No need to sanity check here.
			string color = null;
			string modifier = null;
			if (keyword0 != null) {
				if (ColorKeywords.ContainsKey(keyword0.ToLower())) {
					color = CapitalizeFirst(keyword0);
				} else if (ModifierKeywords.ContainsKey(keyword0.ToLower())) {
					modifier = CapitalizeFirst(keyword0);
				}
			}

			if (keyword1 != null) {
				if (ColorKeywords.ContainsKey(keyword1.ToLower())) {
					color = CapitalizeFirst(keyword1);
				} else if (ModifierKeywords.ContainsKey(keyword1.ToLower())) {
					modifier = CapitalizeFirst(keyword1);
				}
			}

			if (modifier != null) {
				return (modifier ?? "") + (color == null ? "" : (" " + color));
			} else {
				return color ?? "";
			}
		}

		private static string CapitalizeFirst(string inp) {
			if (inp.Length == 0) return inp;
			if (inp.Length == 1) return inp.ToUpper();
			return inp.Substring(0, 1).ToUpper() + inp.Substring(1).ToLower();
		}


		/// <summary>
		/// Hue ranges from 0-360, sat and val are 0-1
		/// </summary>
		/// <param name="color"></param>
		/// <param name="hue"></param>
		/// <param name="saturation"></param>
		/// <param name="value"></param>
		private static void ColorToHSV(Color color, out double hue, out double saturation, out double value) {
			int max = Math.Max(color.R, Math.Max(color.G, color.B));
			int min = Math.Min(color.R, Math.Min(color.G, color.B));

			hue = color.GetHue();
			saturation = (max == 0) ? 0 : 1d - (1d * min / max);
			value = max / 255d;
		}

		/// <summary>
		/// Hue ranges from 0-360, sat and val are 0-1
		/// </summary>
		/// <param name="hue"></param>
		/// <param name="saturation"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static Color ColorFromHSV(double hue, double saturation, double value) {
			int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
			double f = hue / 60 - Math.Floor(hue / 60);

			value = value * 255;
			int v = Convert.ToInt32(value);
			int p = Convert.ToInt32(value * (1 - saturation));
			int q = Convert.ToInt32(value * (1 - f * saturation));
			int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

			if (hi == 0)
				return Color.FromArgb(255, v, t, p);
			else if (hi == 1)
				return Color.FromArgb(255, q, v, p);
			else if (hi == 2)
				return Color.FromArgb(255, p, v, t);
			else if (hi == 3)
				return Color.FromArgb(255, p, q, v);
			else if (hi == 4)
				return Color.FromArgb(255, t, p, v);
			else
				return Color.FromArgb(255, v, p, q);
		}

	}
}

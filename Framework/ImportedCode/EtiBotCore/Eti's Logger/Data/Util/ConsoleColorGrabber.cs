using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using EtiLogger.Data.Structs;

namespace EtiLogger.Data.Util {

	/// <summary>
	/// A utility class that does its best to decide the closest <see cref="ConsoleColor"/> for a given <see cref="Color"/>.
	/// </summary>
	public static class ConsoleColorGrabber {

		/// <summary>
		/// A mapping from <see cref="ConsoleColor"/> to <see cref="Color"/>.
		/// </summary>
		public static readonly IReadOnlyDictionary<ConsoleColor, Color> ConsoleColorToColorMap = new Dictionary<ConsoleColor, Color>() {
			[ConsoleColor.Black] = Color.BLACK,
			[ConsoleColor.DarkBlue] = Color.DARK_BLUE,
			[ConsoleColor.DarkGreen] = Color.DARK_GREEN,
			[ConsoleColor.DarkCyan] = Color.DARK_CYAN,
			[ConsoleColor.DarkRed] = Color.DARK_RED,
			[ConsoleColor.DarkMagenta] = Color.DARK_MAGENTA,
			[ConsoleColor.DarkYellow] = Color.DARK_YELLOW,
			[ConsoleColor.Gray] = Color.GRAY,
			[ConsoleColor.DarkGray] = Color.DARKER_GRAY,
			[ConsoleColor.Blue] = Color.BLUE,
			[ConsoleColor.Green] = Color.GREEN,
			[ConsoleColor.Cyan] = Color.CYAN,
			[ConsoleColor.Red] = Color.RED,
			[ConsoleColor.Magenta] = Color.MAGENTA,
			[ConsoleColor.Yellow] = Color.YELLOW,
			[ConsoleColor.White] = Color.WHITE
		};

		/// <summary>
		/// A cache to store colors that have already been computed.
		/// </summary>
		private static readonly Dictionary<Color, ConsoleColor> ProximityCache = new Dictionary<Color, ConsoleColor>();

		/// <summary>
		/// Returns the <see cref="ConsoleColor"/> most similar to this <see cref="Color"/> via euclidean distance.
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		public static ConsoleColor GetClosestConsoleColor(Color color) {
			if (ProximityCache.ContainsKey(color)) {
				return ProximityCache[color];
			}

			float lowest = 255;
			ConsoleColor retn = ConsoleColor.White;
			Vector3 colorV3 = new Vector3(color.R, color.G, color.B);
			foreach (ConsoleColor clr in ConsoleColorToColorMap.Keys) {
				Color conClr = ConsoleColorToColorMap[clr];
				Vector3 conClrV3 = new Vector3(conClr.R, conClr.G, conClr.B);
				float distance = (colorV3 - conClrV3).Length();
				if (distance < lowest) {
					lowest = distance;
					retn = clr;
				}
			}
			ProximityCache[color] = retn;
			return retn;
		}

		static ConsoleColorGrabber() {
			foreach (ConsoleColor clr in ConsoleColorToColorMap.Keys) {
				ProximityCache[ConsoleColorToColorMap[clr]] = clr;
			}
		}

	}
}

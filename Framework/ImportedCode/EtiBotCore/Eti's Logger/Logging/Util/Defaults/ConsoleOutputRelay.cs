using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiLogger.Data.Util;

namespace EtiLogger.Logging.Util.Defaults {

	/// <summary>
	/// An implementation of <see cref="OutputRelay"/> that references <see cref="Console"/>.<para/>
	/// If the console supports VT sequences, it will make use of them. If not, it will find the closest <see cref="ConsoleColor"/> that represents a given color and use that.
	/// </summary>
	public class ConsoleOutputRelay : OutputRelay {

		/// <inheritdoc/>
		public override void OnBeep(bool shouldBeep, Logger source) {
			if (shouldBeep) Console.Beep();
		}

		/// <inheritdoc/>
		public override void OnLogWritten(LogMessage message, LogLevel messageLevel, bool shouldWrite, Logger source) {
			if (!shouldWrite) return;
			if (Logger.IsVTEnabled) {
				Console.Write(message.ToVTString());
			} else {
				ConsoleColor originalFG = Console.ForegroundColor;
				ConsoleColor originalBG = Console.BackgroundColor;
				foreach (LogMessage.MessageComponent component in message.Components) {
					if (component.Color != null) Console.ForegroundColor = ConsoleColorGrabber.GetClosestConsoleColor(component.Color.Value);
					if (component.BackgroundColor != null) Console.BackgroundColor = ConsoleColorGrabber.GetClosestConsoleColor(component.BackgroundColor.Value);
					// Underline won't work.
					Console.Write(component.Text);
				}
				Console.ForegroundColor = originalFG;
				Console.BackgroundColor = originalBG;
			}
		}
	}
}

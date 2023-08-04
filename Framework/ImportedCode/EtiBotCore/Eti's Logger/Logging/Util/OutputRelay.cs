using System;
using EtiLogger.Logging.Util.Defaults;

namespace EtiLogger.Logging.Util {

	/// <summary>
	/// A class that provides a method of relaying <see cref="Logger"/> activity to any desired source.
	/// </summary>
	public abstract class OutputRelay {

		/// <summary>
		/// The default <see cref="OutputRelay"/>, which points to <see cref="Console"/>.
		/// </summary>
		public static ConsoleOutputRelay ConsoleRelay { get; } = new ConsoleOutputRelay();

		/// <summary>
		/// Executed when something is written to the <paramref name="source"/>. Whether or not this is expected to actually write the text is determined by <paramref name="shouldWrite"/>, which is set based on the log level of the logger vs. the log level of this message.<para/>
		/// In usual cases, appending the log level or a timestamp is not required (it's part of the message that is sent). If you wish to perform this task manually, simply change <see cref="Logger.NoLevel"/> and <see cref="Logger.NoTimestamp"/>.
		/// </summary>
		/// <param name="message">The message to log.</param>
		/// <param name="messageLevel">The <see cref="LogLevel"/> associated with the message.</param>
		/// <param name="shouldWrite">Whether or not <paramref name="messageLevel"/> is less than or equal to the <paramref name="source"/>'s <see cref="Logger.LoggingLevel"/> (meaning that this message should be written, by extension).</param>
		/// <param name="source">The <see cref="Logger"/> that sent this message.</param>
		public abstract void OnLogWritten(LogMessage message, LogLevel messageLevel, bool shouldWrite, Logger source);

		/// <summary>
		/// Executed when a request to play a sound is sent by the <paramref name="source"/>. Whether or not the sound should actually be played is defined by <paramref name="shouldBeep"/>. This sound should be a brief alert sound.
		/// </summary>
		/// <param name="shouldBeep">Whether or not this beep call should actually do anything. This will be false if its associated message sent in <see cref="OnLogWritten"/> has its <c>shouldWrite</c> parameter set to false.</param>
		/// <param name="source">The <see cref="Logger"/> that sent this request.</param>
		public abstract void OnBeep(bool shouldBeep, Logger source);

	}
}

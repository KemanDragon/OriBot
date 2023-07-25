using EtiLogger.Data.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EtiLogger.Logging.Util;

namespace EtiLogger.Logging {

	/// <summary>
	/// A console logging utility. Offers the ability to format messages with color codes.
	/// </summary>
	public class Logger {

		#region Static Logging Information & Init Code

		private readonly static List<Logger> AllInstantiatedLoggers = new List<Logger>();

		/// <summary>
		/// The default logger.
		/// </summary>
		public static Logger Default { get; } = new Logger();

		/// <summary>
		/// Static init code
		/// </summary>
		static Logger() {
#if DEBUG
			if (EnableVTSupport()) {
				Debug.WriteLine("The console supports VT sequences.");
			} else {
				Debug.WriteLine("The console does not support VT sequences.");
			}
#else
			EnableVTSupport();
#endif
		}

		/// <summary>
		/// Creates a new <see cref="Logger"/> with an empty <see cref="LogPrefix"/>.
		/// </summary>
		public Logger() {
			AllInstantiatedLoggers.Add(this);
		}

		/// <summary>
		/// Creates a new <see cref="Logger"/> and sets <see cref="LogPrefix"/> to the given <see cref="string"/>, colored <see cref="Color.WHITE"/>
		/// </summary>
		/// <param name="prefix">The text to set the log's prefix to.</param>
		public Logger(string prefix) : this() {
			LogPrefix = new LogMessage.MessageComponent(prefix, Color.WHITE);
		}

		/// <summary>
		/// Creates a new <see cref="Logger"/> and sets <see cref="LogPrefix"/> to the given <see cref="LogMessage.MessageComponent"/> which may contain custom formats.
		/// </summary>
		/// <param name="styledPrefix"></param>
		public Logger(LogMessage.MessageComponent styledPrefix) : this() {
			LogPrefix = styledPrefix;
		}

		/// <summary>
		/// The time when this class is initialized into memory. Used for the log file name. This value does not change.
		/// </summary>
		private static readonly string CLASS_INIT_TIMESTAMP = DateTime.UtcNow.ToFileTime().ToString();

		/// <summary>
		/// The most detailed message type that can be logged.
		/// </summary>
		public static LogLevel LoggingLevel { get; set; } = LogLevel.Info;

		/// <summary>
		/// The folder that the log file is stored in as a string. Default value is .\ (current EXE directory).
		/// </summary>
		/// <exception cref="InvalidOperationException"/>
		public static string LogContainerFolder {
			get {
				return LogFilePathInternal;
			}
			set {
				if (IsPathLocked) throw new InvalidOperationException("Cannot set the log file path after calling XanBotCoreSystem initialize method.");
				LogFilePathInternal = value;

				if (!LogFilePathInternal.EndsWith("\\")) {
					LogFilePathInternal += "\\";
				}
			}
		}
		internal static string LogFilePathInternal = ".\\";
		internal static bool IsPathLocked = false;

		/// <summary>
		/// The name of the current log file that this <see cref="Logger"/> is writing to.<para/>
		/// This is equal to <see cref="LogContainerFolder"/> + "logfile-" + <see cref="CLASS_INIT_TIMESTAMP"/> + ".log";
		/// </summary>
		public static string LogFilePath => LogContainerFolder + "logfile-" + CLASS_INIT_TIMESTAMP + ".log";

		#endregion

		#region Virtual Terminal

		#region Kernel32 API Imports
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

		static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
		#endregion


		/// <summary>
		/// Whether or not VT Sequences are enabled and should be used.
		/// </summary>
		public static bool IsVTEnabled { get; private set; } = false;

		/// <summary>
		/// When called, this enables VT Sequence support for the console. Whether or not this action will be successful depends on the platform this bot is running on.<para/>
		/// This method will return <see langword="true"/> if VT sequences are supported and enabled, and <see langword="false"/> if they are not.<para/>
		/// VT sequences allow low level control of the console's colors, including the allowance of full 16-million color RGB text and backgrounds.<para/>
		/// See <a href="https://docs.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences">https://docs.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences</a> for more information.
		/// </summary>
		/// <returns>True if VT sequences are supported, false if they are not.</returns>
		public static bool EnableVTSupport() {
			if (IsVTEnabled) return true;

			IntPtr hOut = GetStdHandle(-11);
			if (hOut != INVALID_HANDLE_VALUE) {
				if (GetConsoleMode(hOut, out uint mode)) {
					mode |= 0x4;
					if (SetConsoleMode(hOut, mode)) {
						IsVTEnabled = true;
						return true;
					}
				}
			}
			return false;
		}

		#endregion

		#region Color, Message, and Logging Settings

		#region Defaults

		/// <summary>
		/// The default color used by the console for messages printed with no color defined at <see cref="LogLevel.Info"/>.<para/>
		/// <strong>Default:</strong> <see cref="Color.LIGHTEST_GRAY"/>
		/// </summary>
		public Color DefaultInfoColor { get; set; } = Color.LIGHTEST_GRAY;

		/// <summary>
		/// The default color used by the console for messages printed with no color defined at <see cref="LogLevel.Debug"/>.<para/>
		/// <strong>Default:</strong> <see cref="Color.GRAY"/>
		/// </summary>
		public Color DefaultDebugColor { get; set; } = Color.GRAY;

		/// <summary>
		/// The default color used by the console for messages printed with no color defined at <see cref="LogLevel.Trace"/> or above (if manually defined).<para/>
		/// <strong>Default:</strong> <see cref="Color.DARKER_GRAY"/>
		/// </summary>
		public Color DefaultTraceColor { get; set; } = Color.DARKER_GRAY;

		/// <summary>
		/// The default logging level for this <see cref="Logger"/>.<para/>
		/// <strong>Default:</strong> <see cref="LogLevel.Info"/>
		/// </summary>
		public LogLevel DefaultLevel { get; set; } = LogLevel.Info;

		#endregion

		#region Prefixes

		/// <summary>
		/// A <see cref="string"/> to put before all log entries from this logger (and after the timestamp) (by writing this and then immediately writing logged entries after). This text is appended verbatim, so format codes will not work.<para/>
		/// <strong>Default:</strong> <see langword="new"/> <see cref="LogMessage.MessageComponent"/>(<see cref="string.Empty"/>)<para/>
		/// <strong>Note:</strong> Setting this to <see langword="null"/> will actually set it to the default. As such, this will never be <see langword="null"/>.
		/// </summary>
		public LogMessage.MessageComponent LogPrefix {
			get => _LogPrefix;
			set => _LogPrefix = value ?? new LogMessage.MessageComponent(string.Empty);
		}
		private LogMessage.MessageComponent _LogPrefix = new LogMessage.MessageComponent(string.Empty);

		/// <summary>
		/// If <see langword="true"/>, the timestamp will be omitted from the log.
		/// </summary>
		public bool NoTimestamp { get; set; }

		/// <summary>
		/// If <see langword="true"/>, the level will be omitted from the log (removing prefixes like <c>[INFO]</c>, <c>[DEBUG]</c>, and <c>[TRACE]</c>).
		/// </summary>
		public bool NoLevel { get; set; }

		/// <summary>
		/// If <see langword="true"/>, and if a message that is written to the log contains new lines, then the timestamp will be appended at the start of each line.
		/// </summary>
		public bool AddTimestampToAllNewlines { get; set; } = false;

		/// <summary>
		/// The prefix for <see cref="LogLevel.Info"/> messages.
		/// </summary>
		public string InfoPrefix { get; set; } = "[ OUT ] ";

		/// <summary>
		/// The prefix for <see cref="LogLevel.Debug"/> messages.
		/// </summary>
		public string DebugPrefix { get; set; } = "[DEBUG] ";

		/// <summary>
		/// The prefix for <see cref="LogLevel.Trace"/> messages.
		/// </summary>
		public string TracePrefix { get; set; } = "[TRACE] ";


		#endregion

		/// <summary>
		/// The <see cref="OutputRelay"/> new <see cref="Logger"/>s will be initialized with. Changing this will NOT update any existing <see cref="Logger"/>s.<para/>
		/// <strong>Default:</strong> <see cref="OutputRelay.ConsoleRelay"/>
		/// </summary>
		public static OutputRelay DefaultTarget {
			get => _DefaultTarget;
			set => _DefaultTarget = value ?? throw new ArgumentNullException("value");
		}
		private static OutputRelay _DefaultTarget = OutputRelay.ConsoleRelay;

		/// <summary>
		/// The target <see cref="OutputRelay"/> that this <see cref="Logger"/> will send its text to.<para/>
		/// </summary>
		/// <exception cref="ArgumentNullException">If this is set to null.</exception>
		public OutputRelay Target {
			get => _Target;
			set => _Target = value ?? throw new ArgumentNullException("value");			
		}
		private OutputRelay _Target = DefaultTarget;

		#endregion

		#region Log Writing

		#region Write Strings

		/// <summary>
		/// Log some text on a single line, and make a newline afterwards.
		/// </summary>
		/// <param name="message">The text to log.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteLine(string message = "", LogLevel? logLevel = null, bool alertSound = false) => Write(message + "\n", logLevel, alertSound);

		/// <summary>
		/// Log some text on a single line.
		/// </summary>
		/// <param name="message">The text to log.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void Write(string message = "", LogLevel? logLevel = null, bool alertSound = false) {
			LogLevel trueLevel = logLevel.GetValueOrDefault(DefaultLevel);
			Color headerColor = GetColorForLogLevel(trueLevel);
			LogMessage start = new LogMessage(new LogMessage.MessageComponent("", headerColor));
			Write(start.ConcatLocal(new LogMessage(message)), trueLevel, alertSound);
		}

		/// <summary>
		/// Writes the given text verbatim into the console (does not do any processing of codes e.g. §, ^#XXXXXX;, ^u;), followed by a new line.
		/// </summary>
		/// <param name="text">The text to write to the console</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteLineRaw(string text, LogLevel? logLevel = null) {
			WriteRaw(text + "\n", logLevel);
		}

		#endregion

		#region Write Objects

		#region Write LogMessages

		/// <summary>
		/// Log a complex <see cref="LogMessage"/> object on a single line, and make a newline afterwards.
		/// </summary>
		/// <param name="message">The <see cref="LogMessage"/> to write.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteLine(LogMessage message, LogLevel? logLevel = null, bool alertSound = false) => Write(message.ConcatLocal(new LogMessage("\n")), logLevel, alertSound);

		/// <summary>
		/// Calls <see cref="WriteLine(LogMessage, LogLevel?, bool)"/> with the given parameters, but appends orange text beforehand reading <c>"[WARN] "</c>
		/// </summary>
		/// <param name="message">The <see cref="LogMessage"/> to write.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteWarning(LogMessage message, LogLevel? logLevel = null, bool alertSound = false) {
			WriteLine(new LogMessage(new LogMessage.MessageComponent("[WARN] ", Color.ORANGE)).ConcatLocal(message), logLevel, alertSound);
		}

		/// <summary>
		/// Calls <see cref="WriteLine(LogMessage, LogLevel?, bool)"/> with the given parameters, but appends red text beforehand reading <c>"[SEVERE] "</c>
		/// </summary>
		/// <param name="message">The <see cref="LogMessage"/> to write.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteSevere(LogMessage message, LogLevel? logLevel = null, bool alertSound = false) {
			WriteLine(new LogMessage(new LogMessage.MessageComponent("[SEVERE] ", Color.RED_ORANGE)).ConcatLocal(message), logLevel, alertSound);
		}

		/// <summary>
		/// Calls <see cref="WriteLine(LogMessage, LogLevel?, bool)"/> with the given parameters, but appends red-orange text beforehand reading <c>"[CRITICAL] "</c>
		/// </summary>
		/// <param name="message">The <see cref="LogMessage"/> to write.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteCritical(LogMessage message, LogLevel? logLevel = null, bool alertSound = false) {
			WriteLine(new LogMessage(new LogMessage.MessageComponent("[CRITICAL] ", Color.RED)).ConcatLocal(message), logLevel, alertSound);
		}

		#region Strings for special states

		/// <summary>
		/// Calls <see cref="WriteLine(LogMessage, LogLevel?, bool)"/> with the given parameters, but appends orange text beforehand reading <c>"[WARN] "</c>
		/// </summary>
		/// <param name="message">A <see cref="string"/> that will be used to create a <see cref="LogMessage"/> to write.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteWarning(string message, LogLevel? logLevel = null, bool alertSound = false) {
			WriteLine(new LogMessage(new LogMessage.MessageComponent("[WARN] ", Color.ORANGE)).ConcatLocal(new LogMessage(message)), logLevel, alertSound);
		}

		/// <summary>
		/// Calls <see cref="WriteLine(LogMessage, LogLevel?, bool)"/> with the given parameters, but appends red-orange text beforehand reading <c>"[SEVERE] "</c>
		/// </summary>
		/// <param name="message">A <see cref="string"/> that will be used to create a <see cref="LogMessage"/> to write.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteSevere(string message, LogLevel? logLevel = null, bool alertSound = false) {
			WriteLine(new LogMessage(new LogMessage.MessageComponent("[SEVERE] ", Color.RED_ORANGE)).ConcatLocal(new LogMessage(message)), logLevel, alertSound);
		}

		/// <summary>
		/// Calls <see cref="WriteLine(LogMessage, LogLevel?, bool)"/> with the given parameters, but appends red text beforehand reading <c>"[CRITICAL] "</c>
		/// </summary>
		/// <param name="message">A <see cref="string"/> that will be used to create a <see cref="LogMessage"/> to write.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteCritical(string message, LogLevel? logLevel = null, bool alertSound = false) {
			WriteLine(new LogMessage(new LogMessage.MessageComponent("[CRITICAL] ", Color.RED)).ConcatLocal(new LogMessage(message)), logLevel, alertSound);
		}

		#endregion

		#endregion

		#region Write ILoggables

		/// <summary>
		/// Log an <see cref="ILoggable"/> object on a single line, and make a newline afterwards.
		/// </summary>
		/// <param name="loggable">The object that will provide a <see cref="LogMessage"/> to write.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteLine(ILoggable loggable, LogLevel? logLevel = null, bool alertSound = false) => WriteLine(loggable.ToMessage(), logLevel, alertSound);

		/// <summary>
		/// Log an <see cref="ILoggable"/> object on a single line.
		/// </summary>
		/// <param name="loggable">The object that will provide a <see cref="LogMessage"/> to write.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void Write(ILoggable loggable, LogLevel? logLevel = null, bool alertSound = false) => Write(loggable.ToMessage(), logLevel, alertSound);

		#endregion

		#region Write Exceptions

		/// <summary>
		/// Writes errors to the console and plays a beep sound to alert the operator.<para/>
		/// This has automatic handling for <see cref="AggregateException"/> and <see cref="TypeInitializationException"/>.<para/>
		/// </summary>
		/// <param name="ex">The exception to log.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteException(Exception ex, bool alertSound = true, LogLevel? logLevel = null) {
			WriteLine(ExceptionFormatter.GetExceptionMessage(ex), logLevel, alertSound);
		}

		/// <summary>
		/// Writes errors to the console and plays a beep sound to alert the operator. This expects a manually instantiated exception rather than a thrown one, and does not support <see cref="AggregateException"/>.
		/// </summary>
		/// <param name="ex">The exception to log.</param>
		/// <param name="throwAfter">If true, the exception will be thrown from this method after being written.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteUnthrownException(Exception ex, bool throwAfter = false, bool alertSound = true, LogLevel? logLevel = null) {
			WriteLine(ExceptionFormatter.GetUnthrownExceptionMessage(ex), logLevel, alertSound);
			if (throwAfter) throw ex;
		}

		#endregion

		#endregion

		#region Master Write Methods

		/// <summary>
		/// Log a complex <see cref="LogMessage"/> object on a single line.
		/// </summary>
		/// <param name="message">The <see cref="LogMessage"/> to write.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void Write(LogMessage message, LogLevel? logLevel = null, bool alertSound = false) {
			string timestamp = GetFormattedTimestamp();
			LogLevel trueLevel = logLevel.GetValueOrDefault(DefaultLevel);
			Color headerColor = GetColorForLogLevel(trueLevel);
			bool alreadyHasTimestampAtStart = false;

			if (AddTimestampToAllNewlines) {
				// This is where things get less than ideal to be honest.
				// This is expensive.
				string timestampFormatString = Color.GetFormatString(Color.DARK_GREEN, false, Color.BLACK, headerColor);
				List<LogMessage.MessageComponent> components = new List<LogMessage.MessageComponent>();

				Color lastNonNullFG = headerColor;
				Color lastNonNullBG = Color.BLACK;
				bool lastNonNullUnderline = false;

				for (int idx = 0; idx < message.Components.Count; idx++) {
					LogMessage.MessageComponent component = message.Components[idx];
					lastNonNullFG = component.Color.GetValueOrDefault(lastNonNullFG);
					lastNonNullBG = component.BackgroundColor.GetValueOrDefault(lastNonNullBG);
					lastNonNullUnderline = component.Underline.GetValueOrDefault(lastNonNullUnderline);

					if (!component.Text.Contains("\n")) {
						components.Add(component);
						continue;
					}

					string[] segments = component.Text.Split('\n');
					for (int segIdx = 0; segIdx < segments.Length; segIdx++) {
						string thisSegment = segments[segIdx];
						LogMessage.MessageComponent cmp = new LogMessage.MessageComponent(timestampFormatString + timestamp + Color.GetFormatString(lastNonNullFG, lastNonNullUnderline, lastNonNullBG) + thisSegment + '\n');
						components.Add(cmp);
						if (components.Count == 1) alreadyHasTimestampAtStart = true;
						// This means that the first component we added to the new list was split, and since ^ adds the timestamp, we already have a timestamp at the start of the message.
					}
				}
			}

			bool noTimestamp = NoTimestamp || alreadyHasTimestampAtStart;
			LogMessage newMsg;
			if (IsVTEnabled) {
				newMsg = new LogMessage(Color.VT_RESET);
			} else {
				newMsg = new LogMessage();
			}
			if (!noTimestamp) newMsg.AddComponent("§2" + timestamp);
			newMsg.AddComponent(LogPrefix);
			if (!NoLevel) newMsg.AddComponent(new LogMessage.MessageComponent(GetNameForLogLevel(trueLevel), headerColor));
			newMsg.ConcatLocal(message);

			bool shouldWrite = trueLevel <= LoggingLevel;
			if (alertSound) Target.OnBeep(shouldWrite, this);
			Target?.OnLogWritten(newMsg, trueLevel, shouldWrite, this);
		}

		/// <summary>
		/// Writes the given text verbatim into the console (does not do any processing of codes e.g. §, ^#XXXXXX;, ^u;).
		/// </summary>
		/// <param name="text">The text to write to the console.</param>
		/// <param name="alertSound">If true, this message will cause the console to beep.</param>
		/// <param name="logLevel">The type of log message that this is. If this is <see langword="null"/>, <see cref="DefaultLevel"/> is used.</param>
		public void WriteRaw(string text, LogLevel? logLevel = null, bool alertSound = false) {
			LogLevel trueLevel = logLevel.GetValueOrDefault(DefaultLevel);
			bool shouldWrite = trueLevel <= LoggingLevel;
			if (alertSound) Target.OnBeep(shouldWrite, this);
			Target?.OnLogWritten(LogMessage.WithoutFormatting(text), trueLevel, shouldWrite, this);
		}

		#endregion

		#endregion

		#region Utilities

		/// <summary>
		/// Returns the corresponding default color for the given <see cref="LogLevel"/>
		/// </summary>
		/// <param name="level">The <see cref="LogLevel"/> to get the associated color of.</param>
		/// <returns></returns>
		public Color GetColorForLogLevel(LogLevel level) {
			if (level == LogLevel.Info) {
				return DefaultInfoColor;
			} else if (level == LogLevel.Debug) {
				return DefaultDebugColor;
			}
			return DefaultTraceColor;
		}

		/// <summary>
		/// Returns the name of the log level based on <see cref="InfoPrefix"/>, <see cref="DebugPrefix"/>, and <see cref="TracePrefix"/>.<para/>
		/// The developer may optionally <see langword="override"/> <see cref="GetNameForCustomLogLevel(LogLevel)"/> from which it is possible to provide names for non-standard <see cref="LogLevel"/>s.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		public string GetNameForLogLevel(LogLevel level) {
			//string result = "[" + Enum.GetName(typeof(LogLevel), level) + "] ";
			//if (AddSpaceAfterInfoLevel && level == LogLevel.Info) result += " ";
			//return result;
			if (level == LogLevel.Info) {
				return InfoPrefix;
			} else if (level == LogLevel.Debug) {
				return DebugPrefix;
			} else if (level == LogLevel.Trace) {
				return TracePrefix;
			}
			return GetNameForCustomLogLevel(level);
		}

		/// <summary>
		/// A method that can be overridden to provide a custom prefix for a unique <see cref="LogLevel"/> that is not defined in the enum.<para/>
		/// <strong>Default return value:</strong> "[CUSTOM]"
		/// </summary>
		/// <param name="customLevel"></param>
		/// <returns></returns>
		protected virtual string GetNameForCustomLogLevel(LogLevel customLevel) {
			return "[CUSTOM]";
		}

		/// <summary>
		/// Returns a formatted timestamp: "[HH:MM:SS] "
		/// </summary>
		/// <returns>Returns a formatted timestamp: "[HH:MM:SS] "</returns>
		public static string GetFormattedTimestamp() {
			TimeSpan currentTime = DateTime.Now.TimeOfDay;
			return "[" + currentTime.Hours.ToString("D2") + ":" + currentTime.Minutes.ToString("D2") + ":" + currentTime.Seconds.ToString("D2") + "] ";
		}

		/// <summary>
		/// In case the console runs out of buffer space, clear it out. This is where log files come in handy because the console has limited display.
		/// </summary>
		public static void ClearConsoleIfNecessary() {
			if (Console.CursorTop >= Console.BufferHeight - 50) {
				Console.Clear();
			}
		}

		/// <summary>
		/// Sets the <see cref="Target"/> property of all instantiated <see cref="Logger"/>s to the given <paramref name="target"/>.
		/// </summary>
		/// <param name="target">The new <see cref="OutputRelay"/> to log to.</param>
		/// <exception cref="ArgumentNullException">If the <paramref name="target"/> is null.</exception>
		public static void SetAllLoggerTargetsTo(OutputRelay target) {
			if (target == null) throw new ArgumentNullException(nameof(target));
			foreach (Logger logger in AllInstantiatedLoggers) {
				logger.Target = target;
			}
		}

		#endregion

	}
}

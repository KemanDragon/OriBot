using System;
using System.IO;
using System.Text;
namespace Oribot.Utilities
{
    /// <summary>
    /// A basic logger with support for colors and logs including crash logs.
    /// </summary>
    public class Logger
    {
        /* **** ***** **
        ** DATA TYPES **
        ** **** ***** */
        private enum LogLevel
        {
            DEBUG,
            INFO,
            WARN,
            ERROR,
            FATAL,
            NONE
        }

        /* ********** **
        ** ATTRIBUTES **
        ** ********** */

        // Colors
        private static readonly Color normalColor = new Color(248, 246, 246);
        private static readonly Color warningColor = new Color(245, 208, 97);
        private static readonly Color errorColor = new Color(207, 70, 71);
        private static readonly Color fatalColor = new Color(233, 179, 132); 

        // Config
        private static readonly bool debug = Config.properties["logger"]["debugMode"];

        private static readonly String logFolder = Config.properties["logger"]["logFolder"];
        private static readonly String normalLogFile = Config.properties["logger"]["normalLogFile"];
        private static readonly String debugLogFile = Config.properties["logger"]["debugLogFile"];
        private static readonly String crashLogFile = Config.properties["logger"]["crashLogFile"];

        private static readonly String dateTimeFormat = Config.properties["logger"]["fileDateTimeFormat"];
        private static readonly int crashLogBufferSize = Config.properties["logger"]["crashLogBufferSize"];

        private static readonly bool clumpLogs = Config.properties["logger"]["clumpLevelsTogether"];

        // Printing
        private const int MAX_CAT_SPACE = 7; 

        private static LogLevel previousLogLevel = LogLevel.NONE;
        private static LogLevel currentLogLevel = LogLevel.NONE;

        private static CircularBuffer<String> crashDumpBuffer = new CircularBuffer<String>(crashLogBufferSize); 

        // Writing
        private static String instanceFileIdentifier = null;

        /* ************ **
        ** CONSTRUCTORS **
        ** ************ */

        // To avoid instantiation
        private Logger()
        {
        }

        /// <summary>
        /// Sets up the crash handling.
        /// </summary>
        static Logger() {
            // Important for crash logs
            AppDomain.CurrentDomain.UnhandledException += Logger.DumpCrashLog;
        }

        /* ******* **
        ** METHODS **
        ** ******* */

        /// <summary>
        /// Writes a log to the screen.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="category"></param>
        /// <param name="message"></param>
        private static void _Log(Color color, LogLevel logLevel, String message)
        { 
            String category = null;
            switch (logLevel)
            {
                case LogLevel.DEBUG:
                    category = "debug";
                    break;
                case LogLevel.INFO:
                    category = "info";
                    break;
                case LogLevel.WARN:
                    category = "warning";
                    break;
                case LogLevel.ERROR:
                    category = "error";
                    break;
                case LogLevel.FATAL:
                    category = "fatal";
                    break;
                default:
                    break;
            }

            String unformattedLog = $"[ {category.ToUpper()}{RepeatString(" ", (MAX_CAT_SPACE - category.Length))} ] - {message}";
            crashDumpBuffer.Add(unformattedLog);

            String log = $"{((previousLogLevel != currentLogLevel) && clumpLogs ? "\n" : "")}{color}{unformattedLog}{Color.Reset()}";
            Console.WriteLine(log);
        }

        public static void Debug(String message)
        {
            if (debug)
            {
                currentLogLevel = LogLevel.DEBUG;

                _Log(normalColor, LogLevel.DEBUG, message);
                WriteLogsDebug("debug", message);

                previousLogLevel = currentLogLevel;
            }
        }

        /// <summary>
        /// Creates a normal info level log.
        /// </summary>
        /// <param name="message"></param>
        public static void Log(String message)
        {
            currentLogLevel = LogLevel.INFO;

            if (debug)
            {
                _Log(normalColor, LogLevel.INFO, message);
                WriteLogsDebug("info", message);
            }
            else
            {
                _Log(normalColor, LogLevel.INFO, message);
                WriteLogsNormal("info", message);
            }

            previousLogLevel = currentLogLevel;
        }

        /// <summary>
        /// Creates a warning level log.
        /// </summary>
        /// <param name="message"></param>
        public static void Warning(String message)
        {
            currentLogLevel = LogLevel.WARN;

            if (debug)
            {
                _Log(warningColor, LogLevel.WARN, message);
                WriteLogsDebug("warning", message);
            }
            else
            {
                _Log(warningColor, LogLevel.WARN, message);
                WriteLogsNormal("warning", message);
            }

            previousLogLevel = currentLogLevel;
        }

        /// <summary>
        /// Creates an error level log.
        /// </summary>
        /// <param name="message"></param>
        public static void Error(String message)
        {
            currentLogLevel = LogLevel.ERROR;

            if (debug)
            {
                _Log(errorColor, LogLevel.ERROR, message);
                WriteLogsDebug("error", message);
            }
            else
            {
                _Log(errorColor, LogLevel.ERROR, message);
                WriteLogsNormal("error", message);
            }

            previousLogLevel = currentLogLevel;
        }

        /// <summary>
        /// Creates an fatal level log.
        /// </summary>
        /// <param name="message"></param>
        public static void Fatal(String message)
        {
            currentLogLevel = LogLevel.FATAL;

            if (debug)
            {
                _Log(fatalColor, LogLevel.FATAL, message);
                WriteLogsDebug("fatal", message);
            }
            else
            {
                _Log(fatalColor, LogLevel.FATAL, message);
                WriteLogsNormal("fatal", message);
            }

            previousLogLevel = currentLogLevel;
        }

        /// <summary>
        /// Repeats a given string.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="times"></param>
        /// <returns>String result</returns>
        private static String RepeatString(String message, int times) {
            return new StringBuilder(message.Length * times).Insert(0, message, times).ToString();
        }

        /// <summary>
        /// Writes normal logs to the disk.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="message"></param>
        private static void WriteLogsNormal(String category, String message)
        {
            CheckCreateDirectory();

            String fileName = normalLogFile + "_" + CreateInstanceIdentifier() + ".log";

            String filePath = Path.Combine(Path.Combine(Config.GetRootDirectory(), logFolder), fileName);

            String text = $"[ {category.ToUpper()}{RepeatString(" ", (MAX_CAT_SPACE - category.Length))} ] - {message}\n";

            File.AppendAllText(filePath, text);
        }

        /// <summary>
        /// Writes debug logs to the disk.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="message"></param>
        private static void WriteLogsDebug(String category, String message)
        {
            CheckCreateDirectory();

            String fileName = debugLogFile + "_" + CreateInstanceIdentifier() + ".log";

            String filePath = Path.Combine(Path.Combine(Config.GetRootDirectory(), logFolder), fileName);

            String text = $"[ {category.ToUpper()}{RepeatString(" ", (MAX_CAT_SPACE - category.Length))} ] - {message}\n";

            File.AppendAllText(filePath, text);
        }

        /// <summary>
        /// Dumps the crash log to the disk.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void DumpCrashLog(Object sender, UnhandledExceptionEventArgs e)
        {
            CheckCreateDirectory();

            Logger.Fatal("Program crashed!");

            String fileName = crashLogFile + "_" + CreateInstanceIdentifier() + ".dump";
            String filePath = Path.Combine(Path.Combine(Config.GetRootDirectory(), logFolder), fileName);

            Logger.Log($"Creating dump file at {filePath}...");

            foreach (String log in crashDumpBuffer.ToArray())
            {
                File.AppendAllText(filePath, log + "\n");
            }

            Logger.Log("Dump file created!");
        }

        /// <summary>
        /// Checks whether the log directory exists, else creates it.
        /// </summary>
        private static void CheckCreateDirectory()
        {
            String directoryPath = Path.Combine(Config.GetRootDirectory(), logFolder);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// Creates a unique identifier for files using date time data.
        /// </summary>
        /// <returns></returns>
        private static String CreateInstanceIdentifier()
        {
            return DateTime.Now.ToString(dateTimeFormat);
        }
    }

    /// <summary>
    /// A data structure that represents a ring/circular buffer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class CircularBuffer<T>
    {
        /* ********** **
        ** ATTRIBUTES **
        ** ********** */
        private T[] buffer;
        private int head;
        private int count;

        /* *********** **
        ** CONSTRUCTOR **
        ** *********** */

        /// <summary>
        /// Creates an empty buffer with a given size.
        /// </summary>
        /// <param name="capacity"></param>
        public CircularBuffer(int capacity)
        {
            buffer = new T[capacity];
            head = 0;
            count = 0;
        }

        /* ******* **
        ** METHODS **
        ** ******* */

        /// <summary>
        /// Adds an item to the buffer. If the buffer is overloaded, starts replacing the values from the start.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            buffer[head] = item;
            head = (head + 1) % buffer.Length;

            if (count < buffer.Length)
                count++;
        }

        /// <summary>
        /// Returns an organized array from the buffer.
        /// </summary>
        /// <returns>T[] array</returns>
        public T[] ToArray()
        {
            T[] organizedArray = new T[count];

            int start = (head - count + buffer.Length) % buffer.Length;
            for (int i = 0; i < count; i++)
            {
                organizedArray[i] = buffer[(start + i) % buffer.Length];
            }

            return organizedArray;
        }
    }
}

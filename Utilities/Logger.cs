using System;
using System.IO;
using System.Text;

namespace OriBot.Utilities
{
    /// <summary>
    /// A basic logger with support for colors and logs including crash logs.
    /// </summary>
    public class Logger
    {
        private enum LogLevel
        {
            DEBUG,
            INFO,
            WARN,
            ERROR,
            FATAL
        }

        #region Attributes
        // Colors
        private static readonly Color infoColor = new Color(19, 237, 88);
        private static readonly Color warningColor = new Color(224, 162, 16);
        private static readonly Color errorColor = new Color(250, 50, 50);
        private static readonly Color fatalColor = new Color(153, 8, 8);

        // Config
        private static readonly bool debug = Config.properties["logger"]["debugMode"];

        private static readonly String logFolder = Config.properties["logger"]["logFolder"];
        private static readonly String logFile = Config.properties["logger"]["normalLogFile"];
        private static readonly String debugLogFile = Config.properties["logger"]["debugLogFile"];
        private static readonly String crashLogFile = Config.properties["logger"]["crashLogFile"];

        private static readonly String dateTimeFormat = Config.properties["logger"]["fileDateTimeFormat"];
        private static readonly int crashLogBufferSize = Config.properties["logger"]["crashLogBufferSize"];

        private static readonly bool clumpLogs = Config.properties["logger"]["clumpLevelsTogether"];

        // Printing
        private const int MAX_CAT_SPACE = 7;

        private static LogLevel previousLogLevel = LogLevel.DEBUG;
        private static LogLevel currentLogLevel = LogLevel.DEBUG;

        private static CircularBuffer<String> crashDumpBuffer = new CircularBuffer<String>(crashLogBufferSize);

        // Writing
        private static String instanceFileIdentifier = null;
        
        #endregion


        // To avoid instantiation
        private Logger() { }

        /// <summary>
        /// Sets up the crash handling.
        /// </summary>
        static Logger()
        {
            // Important for crash logs
            AppDomain.CurrentDomain.UnhandledException += Logger.DumpCrashLog;
        }

        # region Methods

        /// <summary>
        /// Writes a log to the screen.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="category"></param>
        /// <param name="writeline"></param>
        private static void _Log(Color color, LogLevel logLevel, String writeline)
        {
            String category = null;
            switch (logLevel)
            {
                case LogLevel.DEBUG:
                    category = "\x1b[94m[DEBUG]";
                    break;
                case LogLevel.INFO:
                    category = "\x1b[92m[INFO]";
                    break;
                case LogLevel.WARN:
                    category = "\x1b[38;5;208m[WARNING]";
                    break;
                case LogLevel.ERROR:
                    category = "\x1b[31m[ERROR]";
                    break;
                case LogLevel.FATAL:
                    category = "\x1b[38;5;124m[FATAL]";
                    break;
                default:
                    break;
            }

            String unformattedLog = $"{category}\x1b[0m {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {writeline}";
            crashDumpBuffer.Add(unformattedLog);

            // String log = $"{((previousLogLevel != currentLogLevel) && clumpLogs ? "\n" : "")}{color}{unformattedLog}{Color.Reset()}";
            // Console.WriteLine(log);
            Console.WriteLine(unformattedLog);
        }

        public static void Debug(String writeline)
        {
            if (debug)
            {
                currentLogLevel = LogLevel.DEBUG;

                _Log(infoColor, LogLevel.DEBUG, writeline);
                WriteDebugLogs($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}", writeline);

                previousLogLevel = currentLogLevel;
            }
        }

        /// <summary>
        /// Creates a normal info level log.
        /// </summary>
        /// <param name="writeline"></param>
        public static void Info(String writeline)
        {
            currentLogLevel = LogLevel.INFO;

            // if (debug)
            // {
            //     _Log(normalColor, LogLevel.INFO, writeline);
            //     WriteLogsDebug("info", writeline);
            // }
            // else
            // {
            _Log(infoColor, LogLevel.INFO, writeline);
            WriteLogs($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss}", writeline);
            // }

            previousLogLevel = currentLogLevel;
        }

        /// <summary>
        /// Creates a warning level log.
        /// </summary>
        /// <param name="writeline"></param>
        public static void Warning(String writeline)
        {
            currentLogLevel = LogLevel.WARN;

            // if (debug)
            // {
            //     _Log(warningColor, LogLevel.WARN, writeline);
            //     WriteLogsDebug("warning", writeline);
            // }
            // else
            // {
            _Log(warningColor, LogLevel.WARN, writeline);
            WriteLogs("[WARNING]", writeline);
            // }

            previousLogLevel = currentLogLevel;
        }

        /// <summary>
        /// Creates an error level log.
        /// </summary>
        /// <param name="writeline"></param>
        public static void Error(String writeline)
        {
            currentLogLevel = LogLevel.ERROR;

            // if (debug)
            // {
            //     _Log(errorColor, LogLevel.ERROR, writeline);
            //     WriteLogsDebug("error", writeline);
            // }
            // else
            // {
            _Log(errorColor, LogLevel.ERROR, writeline);
            WriteLogs("[ERROR]", writeline);
            // }

            previousLogLevel = currentLogLevel;
        }

        /// <summary>
        /// Creates an fatal level log.
        /// </summary>
        /// <param name="writeline"></param>
        public static void Fatal(String writeline)
        {
            currentLogLevel = LogLevel.FATAL;

            // if (debug)
            // {
            //     _Log(fatalColor, LogLevel.FATAL, writeline);
            //     WriteLogsDebug("fatal", writeline);
            // }
            // else
            // {
            _Log(fatalColor, LogLevel.FATAL, writeline);
            WriteLogs("[FATAL]", writeline);
            // }

            previousLogLevel = currentLogLevel;
        }

        /// <summary>
        /// Writes normal logs to the disk.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="writeline"></param>
        private static void WriteLogs(String category, String writeline)
        {
            CheckCreateDirectory();

            // String fileName = logFile + "_" + CreateInstanceIdentifier() + ".log";
            String fileName = logFile + "latest" + ".log";

            String filePath = Path.Combine(Path.Combine(Config.GetRootDirectory(), logFolder), fileName);

            String text = $"{category} - {writeline}\n";

            File.AppendAllText(filePath, text);
        }

        /// <summary>
        /// Writes debug logs to the disk.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="writeline"></param>
        private static void WriteDebugLogs(String category, String writeline)
        {
            CheckCreateDirectory();

            String fileName = "latest" + ".log";

            String filePath = Path.Combine(Path.Combine(Config.GetRootDirectory(), logFolder), fileName);

            String text = $"{category} {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {writeline}\n";

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

            Logger.Info($"Creating dump file at {filePath}...");

            foreach (String log in crashDumpBuffer.ToArray())
            {
                File.AppendAllText(filePath, log + "\n");
            }

            Logger.Info("Dump file created!");
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

        // TODO: Renames latest.log into the date

        internal static void Cleanup()
        {
            CheckCreateDirectory();

            String logFolder = "logs";
            String oldLogFolder = Path.Combine(Config.GetRootDirectory(), logFolder, "old");

            if (!Directory.Exists(oldLogFolder))
            {
                Directory.CreateDirectory(oldLogFolder);
            }

            String existingFileName = "latest.log";
            String newFileName = $"{CreateInstanceIdentifier()}.log";

            String existingFilePath = Path.Combine(Path.Combine(Config.GetRootDirectory(), logFolder), existingFileName);
            String newFilePath = Path.Combine(oldLogFolder, newFileName);

            if (File.Exists(existingFilePath))
            {
                // Move the existing log file to the "old" folder
                File.Move(existingFilePath, newFilePath);
            }
        }

        #endregion
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
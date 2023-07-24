using System;
using System.IO;
using System.Text;
namespace Oribot.Utilities
{
    #region Logger
    /// <summary>
    /// A basic logger with support for colors and logs including crash logs.
    /// </summary>
    public class Logger
    {
        /* ********** **
        ** ATTRIBUTES **
        ** ********** */

        // Colors
        private static readonly Color infoColor = new Color(248, 246, 246);
        private static readonly Color warningColor = new Color(245, 208, 97);
        private static readonly Color errorColor = new Color(207, 70, 71);
        private static readonly Color fatalColor = new Color(255, 0, 255);

        // Config
        private static readonly bool debug = Config.properties["logger"]["debugMode"];

        private static readonly String logFolder = Config.properties["logger"]["logFolder"];
        private static readonly String normalLogFile = Config.properties["logger"]["normalLogFile"];
        private static readonly String debugLogFile = Config.properties["logger"]["debugLogFile"];
        private static readonly String crashLogFile = Config.properties["logger"]["crashLogFile"];

        private static readonly String dateTimeFormat = Config.properties["logger"]["fileDateTimeFormat"];
        private static readonly int crashLogBufferSize = Config.properties["logger"]["crashLogBufferSize"];

        // Printing
        private const int MAX_CAT_SPACE = 7;

        private static int previousLogLevel = 0; // 0 = Normal, ... TODO: Change to enum
        private static int currentLogLevel = 0;

        private static CircularBuffer<String> crashDumpBuffer = new CircularBuffer<String>(crashLogBufferSize);

        // Writing
        private static String instanceFileIdentifier = null;

        /* ************ **
        ** CONSTRUCTORS **
        ** ************ */

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

        /* ******* **
        ** METHODS **
        ** ******* */

        /// <summary>
        /// Writes a log to the screen.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="category"></param>
        /// <param name="message"></param>
        private static void _Log(Color color, String category, String message)
        {
            String unformatted = $"[ {category.ToUpper()}{RepeatString(" ", (MAX_CAT_SPACE - category.Length))} ] - {message}";
            // TODO: Add config clumping bool
            String log = $"{(previousLogLevel != currentLogLevel ? "" : "")}{color}{unformatted}{Color.Reset()}";

            crashDumpBuffer.Add(unformatted);

            Console.WriteLine(log);
        }

        /// <summary>
        /// Creates a normal info level log.
        /// </summary>
        /// <param name="message"></param>
        public static void Info(String message)
        {
            currentLogLevel = 0;

            if (debug)
            {
                _Log(infoColor, "info", message);
                WriteLogsDebug("info", message);
            }
            else
            {
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
            currentLogLevel = 1;

            if (debug)
            {
                _Log(warningColor, "warning", message);
                WriteLogsDebug("warning", message);
            }
            else
            {
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
            currentLogLevel = 2;

            if (debug)
            {
                _Log(errorColor, "error", message);
                WriteLogsDebug("error", message);
            }
            else
            {
                WriteLogsNormal("error", message);
            }

            previousLogLevel = currentLogLevel;
        }

        /**
            TODO: Add FATAL (unless this was done by line 57)
        */
        // public static void Fatal(String message)
        // {
        //     currentLogLevel = 3;

        //     if (debug)
        //     {
        //         _Log(errorColor, "error", message);
        //         WriteLogsDebug("error", message);
        //     }
        //     else
        //     {
        //         WriteLogsNormal("error", message);
        //     }

        //     previousLogLevel = currentLogLevel;
        // }


        /// <summary>
        /// Creates an error level log.
        /// </summary>
        /// <param name="message"></param>

        /// <summary>
        /// Repeats a given string.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="times"></param>
        /// <returns>String result</returns>
        private static String RepeatString(String message, int times)
        {
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

            Logger.Error("Program crashed!");

            // TODO: Dump circular buffer 
            String fileName = crashLogFile + "_" + CreateInstanceIdentifier() + ".dump";
            String filePath = Path.Combine(Path.Combine(Config.GetRootDirectory(), logFolder), fileName);

            Logger.Info($"Creating dump file at {filePath}...");

            foreach (String log in crashDumpBuffer.ToArray())
            {
                File.AppendAllText(filePath, log + "\n");
            }

            Logger.Info("Dump file created!");
            Logger.Warning("Exiting...");
            Environment.Exit(0);
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
    #endregion

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

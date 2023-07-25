using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace OriBot.Framework
{
    public class Logging
    {
        private static Logger logger;

        static Logging() // dont remove this (reminder for slam)
        {
            logger = new Logger();
        }

        public static void Debug(string message)
        {
            logger.Debug(message);
        }

        public static void Info(string message)
        {
            logger.Info(message);
        }

        public static void Warn(string message)
        {
            logger.Warn(message);
        }

        public static void Error(string message)
        {
            logger.Error(message);
        }

        public static void Cleanup()
        {
            logger.tryPack();
        }
    }

    public enum LogLevel
    {
        DEBUG,
        INFO,
        WARN,
        ERROR
    }

    public sealed class Logger
    {
        // FIXME: Add to config
        public bool isDebug = true;
        private string _filePath { get; set; }
        private string _appName = "Oribot-v5.0.0";
        private string _appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);


        public Logger()
        {
            string logFolderPath = Path.Combine(_appDataFolder, _appName, "Logs");

            // create the directory if it doesn't exist
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
                
            }

            this._filePath = Path.Combine(logFolderPath, "latest.log");
        }

        public void tryPack()
        {
            // FIXME:
            // can you, get this working properly xd
            // I aim it to go like uhhhh
            // \OribotAppdataFolder (we can change that later)
            //      L latest.log
            //      L \.old 
            //          L {date}.gz
            //
            string compressedFolderPath = Path.Combine(_appDataFolder, _appName, "Logs", ".old");

            if (!Directory.Exists(compressedFolderPath))
            {
                Directory.CreateDirectory(compressedFolderPath);
            }

            string newName = Path.Combine(_filePath, $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}.log");

            RenameFile(_filePath, newName);
            CompressLogFile(_filePath, compressedFolderPath);
        }

        private void CompressLogFile(string sourceFilePath, string compressedFilePath)
        {
            try
            {
                using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (var compressedFileStream = new FileStream(compressedFilePath, FileMode.Create, FileAccess.Write))
                    {
                        using (var gzipStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                        {
                            sourceStream.CopyTo(gzipStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error compressing the log file: {ex.Message}");
            }
        }

        private void RenameFile(string currentFilePath, string newFilePath)
        {
            try
            {
                File.Move(currentFilePath, newFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error renaming the file: {ex.Message}");
            }
        }


        private void Log(LogLevel level, string message)
        {
            //$"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [{level}] [{origin}] {message}";
            string logEntry = $"[{level}] {message}";

            // Write to console
            System.Console.WriteLine(logEntry);

            // Write to log file
            using (StreamWriter writer = File.AppendText(this._filePath))
            {
                writer.WriteLine(logEntry);
            }
        }

        public void Debug(string message)
        {
            if (isDebug)
            {
                Log(LogLevel.DEBUG, message);
            }
            return;
        }

        public void Info(string message)
        {
            Log(LogLevel.INFO, message);
        }

        public void Warn(string message)
        {
            Log(LogLevel.WARN, message);
        }

        public void Error(string message)
        {
            Log(LogLevel.ERROR, message);
        }
    }
}
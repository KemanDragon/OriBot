using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace Utilities
{
    public class Logger
    {
        private JsonElement logFilePaths = JsonDocument.Parse(File.ReadAllText("config.json")).RootElement.GetProperty("loggerFiles");

        private Color normalColor = new Color(169, 169, 169);
        private Color warnColor = new Color(255, 140, 0);
        private Color errorColor = new Color(220, 20, 60);

        public Task Log(String message)
        {
            Console.WriteLine($"{normalColor}[ LOG ] {message}");
            Console.ResetColor();

            WriteLog("normal", message);

            return Task.CompletedTask;
        } 

        public Task Warn(String warning)
        {
            Console.WriteLine($"{warnColor}[ WRN ] {warning}");
            Console.ResetColor();

            WriteLog("warn", warning);

            return Task.CompletedTask;
        }

        public Task Error(String error)
        {
            Console.WriteLine($"{errorColor}[ ERR ] {error}");
            Console.ResetColor();

            WriteLog("error", error);

            return Task.CompletedTask;
        }

        private Task WriteLog(String type, String log)
        {
            // Get the current time
            String dateTimeData = DateTime.Now.ToString("[yyyy][MMM d] HH:mm:ss - ");

            // Check whether the logs directory exists
            if (!Directory.Exists(logFilePaths.GetProperty("folder").ToString()))
            {
                Directory.CreateDirectory(logFilePaths.GetProperty("folder").ToString());
            }

            // Check whether the type of log is valid
            if (logFilePaths.TryGetProperty(type, out JsonElement fileName))
            {
                String filePath = logFilePaths.GetProperty("folder").ToString() + "/" + fileName.ToString();

                using (StreamWriter writer = File.AppendText(filePath))
                {
                    writer.WriteLine(dateTimeData + log);
                }
            } else 
            {
                // Wrong type of log
                Console.WriteLine($"{errorColor}Unable to write to file, invalid log type!");
            }
            

            return Task.CompletedTask;
        }
    }
}

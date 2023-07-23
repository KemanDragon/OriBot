using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using OriBot.Storage;

namespace Oribot.Utilities
{
    class Config
    {
        // To avoid external instantiation
        private Config() { }

        public static JObject properties;

        public static void LoadConfig()
        {
            // Clear the console once done
            Console.WriteLine("Loading configuration...");

            // Get the config.json file path
            String projectDirectory = GetRootDirectory();
            String configFilePath = Path.Combine(projectDirectory, "config.json");

            // Create JObject instance
            properties = JObject.Load(File.ReadAllText(configFilePath));

            // Clear the console once done
            Console.WriteLine("Configuration loaded!");
        }

        static Config()
        { 
            LoadConfig();
        }

        public static string GetRootDirectory()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string rootDirectory = currentDirectory;

            while (!string.IsNullOrEmpty(rootDirectory))
            {
                string[] projectFiles = Directory.GetFiles(rootDirectory, "*.csproj");
                if (projectFiles.Length > 0)
                {
                    return rootDirectory;
                }

                rootDirectory = Directory.GetParent(rootDirectory)?.FullName;
            }

            throw new Exception("Could not find the root directory of the project.");
        }
    }
}

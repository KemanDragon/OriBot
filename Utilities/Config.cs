using System;
using System.IO;
using OriBot.Storage;

namespace Oribot.Utilities
{
    /// <summary>
    /// Contains the project configuration.
    /// </summary>
    class Config
    {
        /* ********** **
        ** ATTRIBUTES **
        ** ********** */
        public static JObject properties;

        /* ************ **
        ** CONSTRUCTORS **
        ** ************ */
        static Config()
        {
            LoadConfig();
        }

        // Private contructor to avoid external instantiation
        private Config() { }

        /* ******* **
        ** METHODS **
        ** ******* */

        /// <summary>
        /// Loads the project configuration file.
        /// </summary>
        public static void LoadConfig()
        {
            // Get the config.json file path
            String projectDirectory = GetRootDirectory();
            String configFilePath = Path.Combine(projectDirectory, "config.json");

            // Update properties to load the json data
            properties = JObject.Load(File.ReadAllText(configFilePath));
        }

        /// <summary>
        /// Gets the root directory of the project.
        /// </summary>
        /// <returns>String: Root Dir</returns>
        /// <exception cref="Exception"></exception>
        public static string GetRootDirectory()
        {
            // Store the current root directory
            string rootDirectory = Directory.GetCurrentDirectory();

            // Loop till there is a valid root directory
            while (!string.IsNullOrEmpty(rootDirectory))
            {
                // Check whether there are any project files in the root directory
                string[] projectFiles = Directory.GetFiles(rootDirectory, "*.csproj");
                if (projectFiles.Length > 0)
                {
                    return rootDirectory;
                }

                // Change the root directory to the parent directory
                rootDirectory = Directory.GetParent(rootDirectory)?.FullName;
            }

            // If no root director was found throw an exception
            throw new Exception("Could not find the root directory of the project.");
        }
    }
}

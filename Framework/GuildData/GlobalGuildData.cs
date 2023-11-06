using System;
using System.Collections.Generic;
using System.IO;
using OriBot.Utilities;

namespace OriBot.GuildData
{
    public static class GlobalGuildData {
        private static Dictionary<ulong, Dictionary<string, object>> GuildData = null;

        private static string StorageFolderName = Config.properties["guildDataFolderName"];

        private static string BaseStorageDir => Path.Combine(Environment.CurrentDirectory, "Data", StorageFolderName);

        public static string CurrentFileName => Path.Combine(BaseStorageDir, $"GuildData.json");

        public static IReadOnlyDictionary<string, object> GetPerGuildData(ulong id) {
            if (GuildData == null) {
                Initialize();
            }
            if (!GuildData.ContainsKey(id)) {
                GuildData.Add(id, new());
            }
            return GuildData[id];
        }

        public static T GetValueFromData<T>(ulong id, string keyname) {
            return (T)GetPerGuildData(id)[keyname];
        }

        public static void SetValue(ulong id, string keyname, object value) {
            if (GuildData == null)
            {
                Initialize();
            }
            if (!GuildData.ContainsKey(id))
            {
                GuildData.Add(id, new());
            }
            GuildData[id][keyname] = value;
            File.WriteAllText(CurrentFileName, JSON.stringify(GuildData));
        }

        private static void Initialize() {
            if (!File.Exists(CurrentFileName)) {
                Directory.CreateDirectory(Path.GetDirectoryName(CurrentFileName));
                File.WriteAllText(CurrentFileName, "{}");
            }
            GuildData = JSON.parse(File.ReadAllText(CurrentFileName)).ToObject<Dictionary<ulong, Dictionary<string, object>>>();
        }
    }
}
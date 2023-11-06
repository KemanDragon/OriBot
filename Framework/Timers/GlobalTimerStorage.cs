using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using OriBot;
using OriBot.Commands;
using OriBot.EventHandlers;
using OriBot.Framework;
using OriBot.Framework.UserProfiles;
using OriBot.Framework.UserProfiles.SaveableTimer;
using OriBot.PassiveHandlers;
using OriBot.Storage;
using OriBot.Utilities;

namespace main
{
    public static class GlobalTimerStorage
    {
        private static string StorageFolderName = Config.properties["timersFolderName"];
        private static string BaseStorageDir => Path.Combine(Environment.CurrentDirectory, "Data", StorageFolderName);
        private static List<SaveableTimer> _timers = new List<SaveableTimer>();
        public static List<SaveableTimer> Timers
        {
            get
            {
                return _timers;
            }
        }

        public static void AddTimer(SaveableTimer timer)
        {
            _timers.Add(timer);
            Save();
        }

        public static SaveableTimer GetTimerByID(string instanceid, bool remove = false)
        {
            var timer = _timers.FirstOrDefault(x => x.InstanceUID == instanceid);
            if (remove) {
                if (_timers.RemoveAll(x => x.InstanceUID == instanceid) > 0)
                {
                    File.Delete(Path.Combine(BaseStorageDir, $"{instanceid}.json"));
                }
                Save();
            }
            return timer;
        }

        public static void Save() {
            if (!Directory.Exists(BaseStorageDir))
            {
                Directory.CreateDirectory(BaseStorageDir);
            }
            foreach (var timer in _timers) {
                File.WriteAllText(Path.Combine(BaseStorageDir, $"{timer.InstanceUID}.json"), timer.Save());
            }
        }

        public static void Load()
        {
            if (!Directory.Exists(BaseStorageDir))
            {
                Directory.CreateDirectory(BaseStorageDir);
            }
            foreach (var file in Directory.GetFiles(BaseStorageDir))
            {
                var timer = SaveableTimerRegistry.LoadTimerFromString(File.ReadAllText(file));
                _timers.Add(timer);
            }
        }
    }
}
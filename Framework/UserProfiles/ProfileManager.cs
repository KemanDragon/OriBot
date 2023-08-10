using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Discord;
using Discord.WebSocket;
using ICSharpCode.SharpZipLib.Zip;

namespace OriBot.Framework.UserProfiles
{
    public static class ProfileManager
    {
        private static Dictionary<ulong, UserProfile> ProfileCache = new();

        private const int MINUTE = 60 * 1000;

        private static Timer timer = new Timer(MINUTE * 10);

        private static Dictionary<ulong, UserProfile> AutoSaveQueue = new();

        public static void StartTimers()
        {
            timer.AutoReset = true;
            timer.Elapsed += RunAutosave;
        }

        private static void RunAutosave(object sender, ElapsedEventArgs e)
        {
            foreach (var item in AutoSaveQueue)
            {
                item.Value.Save();
            }
            AutoSaveQueue.Clear();
        }

        public static void StopTimers() { timer.Stop(); }

        public static void SaveAllNow() {
            RunAutosave(null,null);
        }

        public static UserProfile GetUserProfile(ulong user)
        {
            if (ProfileCache.ContainsKey(user)) return ProfileCache[user];
            var result = UserProfile.GetOrCreateUserProfile(user);
            if (result != null)
            {
                ProfileCache[user] = result;
                return result;
            }
            return null;
        }

        public static void RemoveFrom(ulong userid) {
            if (ProfileCache.ContainsKey(userid)) ProfileCache.Remove(userid);
        }

        public static void QueueAutosave(UserProfile profile)
        {
            AutoSaveQueue[profile.UserID] = profile;
        }

        public static void RemoveFromQueue(UserProfile profile)
        {
            AutoSaveQueue.Remove(profile.UserID);
        }
    }
}
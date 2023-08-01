using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

using Discord.WebSocket;

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

        public static UserProfile GetUserProfile(SocketUser user)
        {
            if (ProfileCache.ContainsKey(user.Id)) return ProfileCache[user.Id];
            var result = UserProfile.GetOrCreateUserProfile(user);
            if (result != null)
            {
                ProfileCache[user.Id] = result;
                return result;
            }
            return null;
        }

        public static void QueueAutosave(UserProfile profile)
        {
            AutoSaveQueue[profile.Member.Id] = profile;
        }

        public static void RemoveFromQueue(UserProfile profile)
        {
            AutoSaveQueue.Remove(profile.Member.Id);
        }
    }
}
using System;

using Discord.WebSocket;

using OriBot.Framework.UserProfiles;
using OriBot.Framework.UserProfiles.PerGuildData;

namespace OriBot.Framework
{
    public class OricordContext : Context
    {
        public PerGuildData GetPerGuildData(ulong serverID, SocketUser user)
        {
            return ProfileManager.GetUserProfile(user).PerGuildData[serverID];
        }
    }
}
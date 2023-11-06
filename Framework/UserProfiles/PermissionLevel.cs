using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OriBot.Framework.UserProfiles
{
    public enum PermissionLevel
    {
        NewUser = 0,
        Member = 1,
        VIP = 2,
        Moderator = 3,
        BotAdmin = 4,
        BotAndServerOwner = 5
    }
}
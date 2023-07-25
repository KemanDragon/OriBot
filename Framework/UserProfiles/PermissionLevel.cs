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
        ValidatedUser = 1,
        Moderator = 2,
        BotAdmin = 3,
        BotAndServerOwner = 4


    }
}
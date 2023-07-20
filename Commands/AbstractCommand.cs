using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using OriBot.Framework;

namespace OriBot.Commands
{
    public class Command : ModuleBase<SocketCommandContext>
    {
        public GeneralServerContext Fcontext
        {
            get { return main.Memory.contextStorage[Context.Guild.Id]; }
        }
    }

    public class Command<T> : ModuleBase<SocketCommandContext> where T : GeneralServerContext
    {
        public T Fcontext
        {
            get { return (T)main.Memory.contextStorage[Context.Guild.Id]; }
        }

    }
}

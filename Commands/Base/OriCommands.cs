
using System;
using System.Data;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Framework;

namespace OriBot.Commands
{
    [RequireCorrectServer(1005355539447959552,988594970778804245,1131908192004231178,927439277661515776)]
    public class OricordCommand : InteractionModuleBase {

        public OricordContext DataContext {
            get {
                return (OricordContext)main.Memory.ContextStorage["oricord"];
            }
        }
    }
}
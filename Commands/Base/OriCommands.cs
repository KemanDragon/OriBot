
using System;
using System.Data;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Framework;

namespace OriBot.Commands2
{
    [RequireCorrectServer(1005355539447959552)]
    public class OricordCommand : InteractionModuleBase {

        public OricordContext DataContext {
            get {
                return (OricordContext)main.Memory.ContextStorage["oricord"];
            }
        }
    }
}
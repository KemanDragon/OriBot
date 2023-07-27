using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Commands.RequirementEngine;
using OriBot.Framework;

namespace OriBot.Commands
{
    public interface IRequirementCheck {
        public Requirements GetRequirements();
    }

    [Requirements(typeof(OricordCommand))]
    public class OricordCommand : InteractionModuleBase, IRequirementCheck {

        public OricordContext DataContext {
            get {
                return (OricordContext)main.Memory.ContextStorage["oricord"];
            }
        }

        public virtual Requirements GetRequirements()
        {
            return new Requirements((context, commandinfo, services) => {
                ulong[] servers = { 1005355539447959552, 988594970778804245, 1131908192004231178, 927439277661515776 };
                return servers.Contains(context.Guild.Id);
            });
        }
    }
}
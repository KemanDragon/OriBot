using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Commands.RequirementEngine;
using OriBot.Framework;

namespace OriBot.Commands
{
    [Requirements(typeof(MiscModule))]
    public class MiscModule : OricordCommand {

        
        [SlashCommand("echo", "Echo an input")]
        public async Task Echo(string input)
        {
            
            await RespondAsync(Context.Channel.Name);
            await RespondAsync(input);
        }


        public override Requirements GetRequirements()
        {
            return new Requirements((context, commandinfo, services) =>
            {
                ulong[] servers = { 1005355539447959552, 988594970778804245, 1131908192004231178, 927439277661515776 };
                return servers.Contains(context.Guild.Id);
            });
        }
    }
}
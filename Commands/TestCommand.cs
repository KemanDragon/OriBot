
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Framework;

namespace OriBot.Commands2
{
    
    public class MiscModule : OricordCommand {

        
        [SlashCommand("echo", "Echo an input")]
        public async Task Echo(string input)
        {
            
            await RespondAsync(Context.Channel.Name);
            await RespondAsync(input);
        }

    }
}
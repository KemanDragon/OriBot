using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.Commands;

using OriBot.Framework;

namespace OriBot.Commands
{
    public class TestCommandModule : Command<OricordContext>
    {
        public static Stopwatch pingtime;

        [Command("say")]
        [Summary("Echoes a message.")]
        public async Task SayAsync([Summary("The text to echo")] string echo, [Remainder] string text2)
        {
            await ReplyAsync(echo + "|" + text2);
            return;
        }

        [Command("ping")]
        [Summary("Echoes a message.")]
        public async Task PingAsync()
        {
            pingtime = new Stopwatch();
            pingtime.Start();
            await ReplyAsync("@ping");
            
            return;
        }
    }
}

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
    [Attributes.RequireCorrectServer(1005355539447959552)]
    public class WhatISaidModule : Command<OricordContext>
    {
        public static string whatisaid = "nothing";

        public string whatisaid2 = "nothing";

        [Command("remember")]
        [Summary("Remembers a string")]
        public async Task RememberThis([Summary("The text to remember")] string remember)
        {
            whatisaid = remember;
            whatisaid2 = remember;
            await ReplyAsync("Got it!");
            return;
        }

        [Command("whatisaid")]
        [Summary("Echoes the memorized string back")]
        public async Task GiveItBack()
        {
            
            await ReplyAsync("Here is what you said: "+whatisaid+ " heres another: " +whatisaid2);
            return;
        }
    }
}

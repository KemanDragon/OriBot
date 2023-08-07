using System;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using OriBot.Commands.RequirementEngine;
using OriBot.Framework;
using OriBot.Framework.UserBehaviour;
using OriBot.Framework.UserProfiles.SaveableTimer;

namespace OriBot.Commands
{
    [Requirements(typeof(MiscModule))]
    public class MiscModule : OricordCommand
    {
      //  [SlashCommand("echo", "Echo an input")]
        public async Task Echo(string input)
        {
            await RespondAsync(Context.Channel.Name);
            //await RespondAsync(input);
        }

      //  [SlashCommand("duration", "TestIntervals")]
        public async Task Durtesting(TimeSpan duration)
        {
            await RespondAsync(duration.ToString());
        }

     //   [SlashCommand("testwarn", "Warns a user")]
        public async Task Warn(string reason)
        {
            
            var tmplog = UserBehaviourLogRegistry.CreateLogEntry<ModeratorWarnLogEntry>();
            tmplog.Reason = reason;
            var serialized = tmplog.Save();
            await ReplyAsync($"NAME: {tmplog.Name}, ID: {tmplog.ID}, REASON: {tmplog.Reason}, SERIALIZED: {serialized}");
            tmplog = (ModeratorWarnLogEntry)UserBehaviourLogRegistry.LoadLogEntryFromString(serialized);
            serialized = tmplog.Save();
            await ReplyAsync($"RELOADED FROM STRING: NAME: {tmplog.Name}, ID: {tmplog.ID}, REASON: {tmplog.Reason}, SERIALIZED: {serialized}");
        }

      //  [SlashCommand("testtimer", "Timer testing")]
        public async Task TimerTest()
        {
            var tmp = SaveableTimerRegistry.CreateTimer<ExampleTimer>(DateTime.Now.AddSeconds(10), true);
            var serialized = tmp.Instantiate(true).Save();
            var reopen = SaveableTimerRegistry.LoadTimerFromString(serialized);
        }

      //  [SlashCommand("testbuttons", "Test button")]
        public async Task TestButton()
        {
            var confirmationid = Guid.NewGuid().ToString();
            await RespondAsync("Test button", components: new ComponentBuilder().WithButton("OOOO SHINY!", $"testbutton_{confirmationid}", ButtonStyle.Primary).Build());
        }

        [ComponentInteraction("testbutton_*", true)]
        public async Task EchoButton(string input) {
            await RespondAsync("Button confirmed with confirmation id: " + input);
        }
    }
}
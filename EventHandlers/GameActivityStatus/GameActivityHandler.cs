using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;

using OriBot.EventHandlers.Base;
using OriBot.Utilities;

namespace OriBot.EventHandlers.GameActivityStatus;
public class GameActivityHandler : BaseEventHandler
{
    public DiscordSocketClient Client { get; private set; }

    public override void RegisterEventHandler(DiscordSocketClient client)
    {
        Client = client;
        client.Ready += Client_Ready;
    }

    private static Timer sleep;

    private async Task Client_Ready()
    {
        Logger.Debug("Starting Activity Task");
        await StartRandomStatus();
        await SelectStatus(14);
    }

    private async Task StartRandomStatus()
    {
        sleep = new(150000)
        {
            AutoReset = true,
            Enabled = true
        };
        sleep.Elapsed += Randomize;
    }

    private async void Randomize(object sender, ElapsedEventArgs e)
    {
        // FIXME: assign to verbose
        Logger.Debug("Registering Activity Status");
        Random selector = new();
        var foo = selector.Next(0, 13);
        await SelectStatus(foo);
    }

    private async Task SelectStatus(int foo)
    {
        switch (foo)
        {
            // watching
            case 0:
                await UpdateStatusWatching("the leaves fly by");
                break;
            case 1:
                await UpdateStatusWatching("the waves in the water");
                break;
            case 2:
                await UpdateStatusWatching("the forest");
                break;

            // listening
            case 3:
                await UpdateStatusListening("Naru's stories");
                break;
            case 4:
                await UpdateStatusListening("the wind and the leaves");
                break;
            case 5:
                await UpdateStatusListening("The Spirit Tree");
                break;
            case 6:
                await UpdateStatusListening("the birds by the lake");
                break;

            // playing
            case 7:
                await UpdateStatusPlaying("with Naru");
                break;
            case 8:
                await UpdateStatusPlaying("with Gumo");
                break;
            case 9:
                await UpdateStatusPlaying("with Ku");
                break;
            case 10:
                await UpdateStatusPlaying("with moki");
                break;
            case 11:
                await UpdateStatusPlaying("with friends");
                break;

            // competing
            case 12:
                await UpdateStatusCompeting("the Spirit Trials");
                break;
            case 13:
                await UpdateStatusCompeting("Combat");
                break;

            // ori thud
            default:
                Logger.Debug("No Game Activity is being displayed, calling the spirit well.");
                // await UpdateStatusWatching(null);
                await UpdateStatusWatching("the spirit well");
                break;
        }
    }

    // Ooohh.. repetative methods... compress it?
    private async Task UpdateStatusWatching(string description)
    {
        // FIXME: link to verbose:
        Logger.Debug($"Setting game status to '{ActivityType.Watching} {description}'");
        await Client.SetGameAsync(description, null, ActivityType.Watching);
    }
    private async Task UpdateStatusListening(string description)
    {
        // FIXME: link to verbose:
        Logger.Debug($"Setting game status to '{ActivityType.Listening} {description}'");
        await Client.SetGameAsync(description, null, ActivityType.Listening);
    }
    private async Task UpdateStatusPlaying(string description)
    {
        // FIXME: link to verbose:
        Logger.Debug($"Setting game status to '{ActivityType.Playing} {description}'");
        await Client.SetGameAsync(description, null, ActivityType.Playing);
    }
    private async Task UpdateStatusCompeting(string description)
    {
        // FIXME: link to verbose:
        Logger.Debug($"Setting game status to '{ActivityType.Competing} {description}'");
        await Client.SetGameAsync(description, null, ActivityType.Competing);
    }
}
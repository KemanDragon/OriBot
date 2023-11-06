using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using OriBot.EventHandlers.Base;
using OriBot.Utilities;

namespace OriBot.EventHandlers
{
    public class MemberManipulationHandler : BaseEventHandler
    {
        public DiscordSocketClient Client { get; private set; }

        private ulong GuildCount { get; set; }
        private List<ulong> _guilds = new List<ulong>();
        private static Timer wait;

        public override void RegisterEventHandler(DiscordSocketClient client)
        {
            Client = client;
            Client.GuildMemberUpdated += OnGuildMemberUpdated;
            Client.GuildMembersDownloaded += OnGuildMembersDownloaded;
            _ = AnnounceWatch();
        }

        private Task AnnounceWatch()
        {
            wait = new(2000)
            {
                AutoReset = false,
                Enabled = true
            };
            wait.Elapsed += AnnounceGuild;
            return Task.CompletedTask;
        }

        private void AnnounceGuild(object sender, ElapsedEventArgs e)
        {
            Logger.Info($"Successfully retrieved members from {this.GuildCount} guilds");
        }

        private Task OnGuildMembersDownloaded(SocketGuild guild)
        {
            Logger.Debug("Downloaded guild members for guild: " + guild.Name);
            _guilds.Add(guild.Id);
            this.GuildCount = (ulong)_guilds.Count;
            wait.Interval = 2000;
            return Task.CompletedTask;
        }

        private async Task OnGuildMemberUpdated(Discord.Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
        {
            if (!_guilds.Contains(after.Guild.Id)) return;
            var cached = before.Value.GetGuildAvatarUrl(ImageFormat.Png);
            var embedbuilder2 = new EmbedBuilder()
            .WithAuthor(after)
            .WithTitle($"User {after.Mention} changed server profile info.")
            .WithDescription($"Event Time: <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>")
            .AddField($"Previous nickname: {before.Value.DisplayName}", $"New nickname: {after.DisplayName}")
            .AddField($"Previous guild avatar: {cached}", $"New guild avatar: {after.GetGuildAvatarUrl(ImageFormat.Png)}")
            .WithFooter($"Author ID: {after.Id}");


            if (cached != null && cached != after.GetGuildAvatarUrl(ImageFormat.Png))
            {
                var downloaded = await MessageDeleteHandler.DownloadFile(cached);
                embedbuilder2.WithImageUrl($"attachment://{Path.GetFileName(downloaded)}");
                await (Channels.GetLoggingChannel(after.Guild) as SocketTextChannel).SendFileAsync(downloaded, embed: embedbuilder2.Build());
                return;

            }

            await (Channels.GetLoggingChannel(after.Guild) as SocketTextChannel).SendMessageAsync(embed: embedbuilder2.Build());
        }
    }
}
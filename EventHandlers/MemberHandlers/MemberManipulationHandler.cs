using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using OriBot.EventHandlers.Base;
using OriBot.Utilities;

namespace OriBot.EventHandlers
{
    public class MemberManipulationHandler : BaseEventHandler
    {
        public DiscordSocketClient Client { get; private set; }

        private List<ulong> _guilds = new List<ulong>();

        public override void RegisterEventHandler(DiscordSocketClient client)
        {
            Client = client;
            Client.GuildMemberUpdated += OnGuildMemberUpdated;
            Client.GuildMembersDownloaded += OnGuildMembersDownloaded;
        }

        private Task OnGuildMembersDownloaded(SocketGuild guild)
        {
            Logger.Debug("Downloaded guild members for guild: " + guild.Name);
            _guilds.Add(guild.Id);
            return Task.CompletedTask;
        }

        private async Task OnGuildMemberUpdated(Discord.Cacheable<SocketGuildUser,ulong> before, SocketGuildUser after)
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
            

            if (cached != null && cached != after.GetGuildAvatarUrl(ImageFormat.Png)) {
                var downloaded = await MessageDeleteHandler.DownloadFile(cached);
                embedbuilder2.WithImageUrl($"attachment://{Path.GetFileName(downloaded)}");
                await (Channels.GetLoggingChannel(after.Guild) as SocketTextChannel).SendFileAsync(downloaded, embed: embedbuilder2.Build());
                return;

            }

            await (Channels.GetLoggingChannel(after.Guild) as SocketTextChannel).SendMessageAsync(embed: embedbuilder2.Build());
        }
    }
}
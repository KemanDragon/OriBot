using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using OriBot.EventHandlers.Base;
using OriBot.Utilities;

namespace OriBot.EventHandlers
{
    public class SavedMessage
    {
        public string content = "";

        public List<string> images = new List<string>();
    }

    public static class Channels {
        public static SocketGuildChannel GetLoggingChannel(SocketGuild guild)
        {
            return guild.Channels.Where(x => x.Name == Config.properties["auditing"]["messageactivity"].ToObject<string>()).FirstOrDefault() as SocketGuildChannel;
        }
    }

    public class MessageEditHandler : BaseEventHandler
    {
        private List<long> cachingServers = Config.properties["cachingServers"].ToObject<List<long>>();

        public DiscordSocketClient Client { get; private set; }

        

        public override void RegisterEventHandler(DiscordSocketClient client)
        {
            client.MessageUpdated += Client_MessageUpdated;
            Client = client;
        }

        private async Task Client_MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            var arg = await arg1.GetOrDownloadAsync();
            if (arg2.Channel.Id == arg2.Author.Id) {
                return;
            }
            if (!arg1.HasValue)
            {
                var uncapped = "Message content limited to 1024 chars: \"" + arg2.Content + "\"";
                var capped = uncapped.Substring(0, Math.Min(uncapped.Length, 1024));
                var embedbuilder2 = new EmbedBuilder()
                    .WithAuthor(arg2.Author)
                    .AddField($"Edited message https://discord.com/channels/{(arg3 as SocketTextChannel).Guild.Id}/{(arg3 as SocketTextChannel).Id}/{arg1.Id} in: https://discord.com/channels/{(arg3 as SocketTextChannel).Guild.Id}/{(arg3 as SocketTextChannel).Id}","Previous message cannot be found.")
                    .AddField($"New message contents ", $"{capped}")
                    .AddField($"Edited Time: <t:{ DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>", "Original message Time and Date: none")
                    .WithFooter($"Author ID: {arg2.Author.Id} | Message ID: {arg1.Id}");
                await (Channels.GetLoggingChannel((arg3 as SocketTextChannel).Guild) as SocketTextChannel).SendMessageAsync(embed: embedbuilder2.Build());
                return;
            }
            if (arg2.Author.IsBot)
            {
                return;
            }
            var RemovedAttachments = arg.Attachments.Where(x => !arg2.Attachments.Any(y => y.Id == x.Id)).ToList();
            var uncapped1 = "Message content limited to 1024 chars: \"" + arg1.Value.Content + "\"";
            var capped1 = uncapped1.Substring(0, Math.Min(uncapped1.Length, 1024));
            var uncapped2 = "Message content limited to 1024 chars: \"" + arg2.Content + "\"";
            var capped2 = uncapped2.Substring(0, Math.Min(uncapped2.Length, 1024));
            var embedbuilder = new EmbedBuilder()
                    .WithAuthor(arg2.Author)
                    .AddField($"Edited message https://discord.com/channels/{(arg3 as SocketTextChannel).Guild.Id}/{(arg3 as SocketTextChannel).Id}/{arg1.Id} in: https://discord.com/channels/{(arg3 as SocketTextChannel).Guild.Id}/{(arg3 as SocketTextChannel).Id}", $"{capped1}")
                    .AddField($"New message contents ", $"{capped2}")
                    .AddField($"Edited Time: <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>", $"Original message Time and Date: <t:{arg1.Value.Timestamp.ToUnixTimeSeconds()}>")
                    .AddField($"Removed / changed attachments: ", RemovedAttachments.Count > 0 ? RemovedAttachments.Count : "None")
                    .WithFooter($"Author ID: {arg.Author.Id} | Message ID: {arg1.Id}");
            if (RemovedAttachments.Count > 0)
            {
                var savedimages = await MessageDeleteHandler.DownloadAllAttachments(RemovedAttachments);
                var attachments = savedimages.Select(x => new FileAttachment(x, Path.GetFileName(x))).ToList();
                if (!cachingServers.Contains((long)(arg.Channel as SocketTextChannel).Guild.Id) || attachments.Count == 0)
                {

                    await (Channels.GetLoggingChannel((arg3 as SocketTextChannel).Guild) as SocketTextChannel).SendMessageAsync(embed: embedbuilder.Build());
                    return;
                }
                await (Channels.GetLoggingChannel((arg3 as SocketTextChannel).Guild) as SocketTextChannel).SendFilesAsync(attachments, embed: embedbuilder.Build());
                foreach (var item in attachments)
                {
                    item.Dispose();
                }
                foreach (var item in savedimages)
                {
                    File.Delete(item);
                }
                return;
            }

            await (Channels.GetLoggingChannel((arg3 as SocketTextChannel).Guild) as SocketTextChannel).SendMessageAsync(embed: embedbuilder.Build());
        }
    }

    public class MessageDeleteHandler : BaseEventHandler
    {
        

        private List<long> cachingServers = Config.properties["cachingServers"].ToObject<List<long>>();

        public DiscordSocketClient Client { get; private set; }

        public override void RegisterEventHandler(DiscordSocketClient client)
        {
            client.MessageDeleted += ((Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2) =>
            {
                return Task.Run(() =>
                {
                    _ = Client_MessageDeleted(arg1, arg2);
                });
            });
            Client = client;
        }

        private async Task Client_MessageDeleted(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
        {
            var arg = await arg1.GetOrDownloadAsync();
            if (!arg2.HasValue) {
                return;
            }
            if (!arg1.HasValue)
            {
                var embedbuilder2 = new EmbedBuilder()
                .AddField($"Deleted message in: https://discord.com/channels/{(arg2.Value as SocketTextChannel).Guild.Id}/{(arg2.Value as SocketTextChannel).Id}", "Cannot be recovered.")
                .AddField($"Event Time: <t:{ DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>", "Message Time and Date: none")
                .WithFooter($"Message ID: {arg1.Id}");
             //   (arg2.Value as SocketTextChannel).Guild.Channels.Where(x => x.Name == )
                await (Channels.GetLoggingChannel((arg2.Value as SocketTextChannel).Guild) as SocketTextChannel).SendMessageAsync(embed: embedbuilder2.Build());
                return;
            }

            if (arg.Author.IsBot)
            {
                return;
            }
            var uncapped1 = "Message content limited to 1024 chars: \"" + arg.Content + "\"";
            var capped1 = uncapped1.Substring(0, Math.Min(uncapped1.Length, 1024));
            var embedbuilder = new EmbedBuilder()
                .WithFooter($"Author ID: {arg.Author.Id} | Message ID: {arg.Id}")
                .AddField($"Deleted message in: https://discord.com/channels/{(arg.Channel as SocketTextChannel).Guild.Id}/{(arg.Channel as SocketTextChannel).Id}", capped1)
                .AddField($"Message Time and Date:  <t:{arg.Timestamp.ToUnixTimeSeconds()}>", $"Event Time: <t:{ DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>")
                .WithAuthor(arg.Author);
            if (!Directory.Exists("tempcache"))
            {
                Directory.CreateDirectory("tempcache");
            }
            // Save images.
            var savedimages = await DownloadAllAttachments(arg.Attachments);
            var attachments = savedimages.Select(x => new FileAttachment(x, Path.GetFileName(x))).ToList();
            if (!cachingServers.Contains((long)(arg.Channel as SocketTextChannel).Guild.Id) || attachments.Count == 0) {

                await (Channels.GetLoggingChannel((arg2.Value as SocketTextChannel).Guild) as SocketTextChannel).SendMessageAsync(embed: embedbuilder.Build());
                return;
            }
            await (Channels.GetLoggingChannel((arg2.Value as SocketTextChannel).Guild) as SocketTextChannel).SendFilesAsync(attachments, embed: embedbuilder.Build());
            foreach (var item in attachments)
            {
                item.Dispose();
            }
            foreach (var item in savedimages)
            {
                File.Delete(item);
            }
        }

        private string[] allowedTypes = new string[] {
            ".png",
            ".jpeg",
            ".jpg",
            ".apng",
            ".jpeg-large",
            ".webp",
            ".tiff",
            ".gif",
        };

        private bool IsFileAllowed(string url)
        {
            foreach (var item in allowedTypes)
            {
                if (url.EndsWith(item)) return true;
            }
            return false;
        }

        public static async Task<List<string>> DownloadAllAttachments(IReadOnlyCollection<IAttachment> attachments) {
            var savedimages = new List<string>();
            foreach (var item in attachments)
            {
                try {
                    var guid = Guid.NewGuid().ToString();
                    {
                        using var filestream = new FileStream($"tempcache/{guid}{Path.GetExtension(item.Filename)}", FileMode.Create);
                        using var client = new HttpClient();
                        using var s = await client.GetStreamAsync(item.Url);
                        await s.CopyToAsync(filestream);
                    }
                    savedimages.Add($"tempcache/{guid}{Path.GetExtension(item.Filename)}");
                } catch (Exception e) {
                    Logger.Error("FAILED TO REPLICATE: " + item.Url + " , with exception: " + e);
                }
            }
            return savedimages;
        }
    }
}
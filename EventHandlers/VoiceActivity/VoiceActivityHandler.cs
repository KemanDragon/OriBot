using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using OriBot.EventHandlers.Base;
using OriBot.GuildData;
using OriBot.Utilities;

public class VoiceActivityHandler : BaseEventHandler
{
    public enum EventType {
        Leave,
        Join,
        Move,
        Other
    }


    private DiscordSocketClient Client { get; set; }

    public override void RegisterEventHandler(DiscordSocketClient client)
    {
        Client = client;
        client.UserVoiceStateUpdated += UserVoiceStateUpdated;
    }

    private string GenerateLink(ulong guildid, ulong channelid) {
        return $"https://discord.com/channels/{guildid}/{channelid}";
    }

    public static class Channels
    {
        public static SocketTextChannel GetLoggingChannel(SocketGuild guild)
        {
            if (!GlobalGuildData.GetPerGuildData(guild.Id).ContainsKey("voiceactivity"))
            {
                return (SocketTextChannel)guild.Channels.FirstOrDefault(x => x.Name == Config.properties["auditing"]["voiceactivity"].ToObject<string>());
            }
            return (SocketTextChannel)guild.Channels.FirstOrDefault(x => x.Id == GlobalGuildData.GetValueFromData<ulong>(guild.Id, "voiceactivity"));
        }
    }

    private async Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after) {
        var eventype = EventType.Other;
        
        if (after.VoiceChannel == before.VoiceChannel) {
            eventype = EventType.Other;
        } else if (after.VoiceChannel == null) {
            eventype = EventType.Leave;
        } else if (before.VoiceChannel == null) {
            eventype = EventType.Join;
        } else if (before.VoiceChannel != after.VoiceChannel) {
            eventype = EventType.Move;
        }
        if (eventype == EventType.Other) {
            return;
        }
        var embed = new EmbedBuilder();
        SocketTextChannel result = null;
        switch (eventype)
        {
            case EventType.Leave:
                embed.WithAuthor(user);
                embed.WithTitle("This user left a voice channel");
                embed.AddField("Voice channel", $"{GenerateLink(before.VoiceChannel.Guild.Id,before.VoiceChannel.Id)} / ({before.VoiceChannel.Id})");
                result = Channels.GetLoggingChannel(before.VoiceChannel.Guild);
                break;
            case EventType.Join:
                embed.WithAuthor(user);
                embed.WithTitle("This user joined a voice channel");
                embed.AddField("Voice channel", $"{GenerateLink(after.VoiceChannel.Guild.Id, after.VoiceChannel.Id)} / ({after.VoiceChannel.Id})");
                result = Channels.GetLoggingChannel(after.VoiceChannel.Guild);
                break;
            case EventType.Move:
                embed.WithAuthor(user);
                embed.WithTitle("This user moved between voice channels");
                embed.AddField("Previous voice channel", $"{GenerateLink(before.VoiceChannel.Guild.Id, before.VoiceChannel.Id)} / ({before.VoiceChannel.Id})");
                embed.AddField("New voice channel", $"{GenerateLink(after.VoiceChannel.Guild.Id, after.VoiceChannel.Id)} / ({after.VoiceChannel.Id})");
                result = Channels.GetLoggingChannel(after.VoiceChannel.Guild);
                break;
            default:
                break;
        }
        embed.AddField("Event Time", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
        embed.WithFooter($"Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()} | Person ID: {user.Id}");
        await result.SendMessageAsync(embed: embed.Build());
    }

}
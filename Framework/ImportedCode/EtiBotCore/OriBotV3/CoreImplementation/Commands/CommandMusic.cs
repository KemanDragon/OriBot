using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Data;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Interaction.CommandData;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Extensions;
using OldOriBot.Utility.Music;
using OldOriBot.Utility.Music.FileRepresentation;
using OldOriBot.Utility.Responding;
using Fastenshtein;

namespace OldOriBot.CoreImplementation.Commands
{
    public class CommandMusic : Command
    {
        public override string Name { get; } = "music";
        public override string Description { get; } = "A command that will initialize the radio channel and begin playing music.";
        public override ArgumentMapProvider Syntax { get; }
        public override Command[] Subcommands { get; }
        public MusicController Controller { get; private set; }
        public override bool CanSeeHelpForAnyway { get; } = true;

        public CommandMusic(BotContext ctx) : base(ctx)
        {
            Subcommands = new Command[] {
                new CommandMusicVoteNext(ctx, this),
                new CommandMusicStop(ctx, this),
                new CommandMusicSkip(ctx, this),
                new CommandMusicRestart(ctx, this),
                new CommandMusicForceNext(ctx, this),
                new CommandMusicVoteData(ctx, this),
                new CommandMusicSystemData(ctx, this),
                new CommandMusicMigrate(ctx, this)
            };
        }

        public override CommandUsagePacket CanRunCommand(Member member)
        {
            if (Controller == null) PopulateControllerRef();
            if (Controller == null) return new CommandUsagePacket(false, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
            if (DiscordClient.Current.DevMode && member.GetPermissionLevel() < PermissionLevel.Operator) return new CommandUsagePacket(false, "The bot is currently in debug or developer mode, and as such, music cannot be played.");

            CommandUsagePacket baseUsage = base.CanRunCommand(member);
            if (!baseUsage.CanUse) return baseUsage;

            if (!Controller.IsSystemEnabled) return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.systemIsOff"));

            if (member.GetPermissionLevel() < PermissionLevel.Operator)
            {
                if (member.CurrentVoiceChannel == Controller.EffectiveMusicChannel)
                    return CommandUsagePacket.Success;
                return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.notInMusicChannel"));
            }
            return CommandUsagePacket.Success;
        }

        public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn)
        {
            if (member.GetPermissionLevel() >= PermissionLevel.Operator) return channelUsedIn;
            if (channelUsedIn == executionContext.BotChannelID) return channelUsedIn;
            if (Controller == null) PopulateControllerRef();
            if (Controller == null) throw new CommandException(this, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
            if (channelUsedIn == Controller.MusicTextChannel.ID) return channelUsedIn;
            return Controller.MusicTextChannel.ID;
        }

        /// <summary>
        /// Returns an existing instance of <see cref="MusicController"/> for the <see cref="BotContext"/> this command was instantiated in, or creates a new one.
        /// </summary>
        /// <returns></returns>
        internal MusicController PopulateControllerRef()
        {
            if (Controller != null) return Controller;

            VoiceChannel musicChannel = null;
            TextChannel textChannel = null;
            if (Context is BotContextTestServer)
            {
                musicChannel = Context.Server.GetChannel<VoiceChannel>(794170577413472276);
                textChannel = Context.Server.GetChannel<TextChannel>(797601510812287027);
            }
            else if (Context is BotContextOriTheGame)
            {
                musicChannel = Context.Server.GetChannel<VoiceChannel>(624819632649273384);
                textChannel = Context.Server.GetChannel<TextChannel>(625489171095748629);
            }
            if (musicChannel != null && textChannel != null)
                Controller = MusicController.GetOrCreate(musicChannel, textChannel);

            return Controller;
        }

        public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole)
        {
            if (Controller == null) PopulateControllerRef();
            if (Controller == null) throw new CommandException(this, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
            if (Controller.Playing) throw new CommandException(this, Personality.Get("cmd.ori.music.err.alreadyPlaying"));
            if (DiscordClient.Current.DevMode && executor.GetPermissionLevel() < PermissionLevel.Operator) throw new CommandException(this, Personality.Get("cmd.ori.music.err.debugActive"));

            _ = Controller.PlayAudio();
            return Task.CompletedTask;
        }

        public class CommandMusicSkip : Command
        {
            public override string Name { get; } = "skip";
            public override string Description { get; } = "Votes to skip the current track.";
            public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<bool>("force").SetRequiredState(false);

            public CommandMusicSkip(BotContext ctx, Command parent) : base(ctx, parent)
            {
            }

            public override bool CanSeeHelpForAnyway { get; } = true;

            public override CommandUsagePacket CanRunCommand(Member member)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null) return new CommandUsagePacket(false, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (DiscordClient.Current.DevMode && member.GetPermissionLevel() < PermissionLevel.Operator) return new CommandUsagePacket(false, "The bot is currently in debug or developer mode, and as such, music cannot be played.");

                CommandUsagePacket baseUsage = base.CanRunCommand(member);
                if (!baseUsage.CanUse) return baseUsage;

                if (!controller.IsSystemEnabled) return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.systemIsOff"));

                if (member.CurrentVoiceChannel == controller.EffectiveMusicChannel) return CommandUsagePacket.Success;
                return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.notInMusicChannel"));
            }

            public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null) throw new CommandException(this, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (member.GetPermissionLevel() >= PermissionLevel.Operator) return channelUsedIn;
                if (channelUsedIn == executionContext.BotChannelID) return channelUsedIn;
                if (channelUsedIn == controller.MusicTextChannel.ID) return channelUsedIn;
                return controller.MusicTextChannel.ID;
            }

            public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole)
            {
                MusicController controller = ((CommandMusic)Parent).Controller;
                if (!controller.Playing) throw new CommandException(this, Personality.Get("cmd.ori.music.err.notConnected"));
                if (controller.Encoding) throw new CommandException(this, Personality.Get("cmd.ori.music.err.busyEncoding"));
                if (DiscordClient.Current.DevMode && executor.GetPermissionLevel() < PermissionLevel.Operator) throw new CommandException(this, Personality.Get("cmd.ori.music.err.debugActive"));

                if (argArray.Length > 1)
                {
                    throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
                }

                ArgumentMap<bool> args = Syntax.Parse<bool>(argArray.ElementAtOrDefault(0));
                if (args.Arg1)
                {
                    if (executor.GetPermissionLevel() >= PermissionLevel.Operator)
                    {
                        await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.skip"), mentions: AllowedMentions.Reply);
                        await ((CommandMusic)Parent).Controller.Skip();
                        return;
                    }
                    else
                    {
                        throw new CommandException(this, Personality.Get("cmd.ori.music.err.invalidPerms", PermissionLevel.Operator.GetFullName()));
                    }
                }
                MusicVotingHelper voter = ((CommandMusic)Parent).Controller.VoteController;
                bool addedVote = voter.AddVoteToMusicSkip(executor);
                if (addedVote)
                {
                    await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.vote.skip.add"), mentions: AllowedMentions.Reply);
                }
                else
                {
                    await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.vote.skip.remove"), mentions: AllowedMentions.Reply);
                }
            }
        }

        public class CommandMusicStop : Command
        {
            public override string Name { get; } = "stop";
            public override string Description { get; } = "Votes to skip the current track.";
            public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<bool, bool>("onNext", "force").SetRequiredState(false, false);
            public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;

            public CommandMusicStop(BotContext ctx, Command parent) : base(ctx, parent)
            {
            }

            public override bool CanSeeHelpForAnyway { get; } = true;

            public override CommandUsagePacket CanRunCommand(Member member)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null) return new CommandUsagePacket(false, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (DiscordClient.Current.DevMode && member.GetPermissionLevel() < PermissionLevel.Operator) return new CommandUsagePacket(false, "The bot is currently in debug or developer mode, and as such, music cannot be played.");

                CommandUsagePacket baseUsage = base.CanRunCommand(member);
                if (!baseUsage.CanUse) return baseUsage;

                if (!controller.IsSystemEnabled) return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.systemIsOff"));

                if (member.GetPermissionLevel() < PermissionLevel.Operator)
                {
                    if (member.CurrentVoiceChannel == controller.EffectiveMusicChannel)
                        return CommandUsagePacket.Success;
                    return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.notInMusicChannel"));
                }
                return CommandUsagePacket.Success;
            }

            public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null) throw new CommandException(this, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (member.GetPermissionLevel() >= PermissionLevel.Operator) return channelUsedIn;
                if (channelUsedIn == executionContext.BotChannelID) return channelUsedIn;
                if (channelUsedIn == controller.MusicTextChannel.ID) return channelUsedIn;
                return controller.MusicTextChannel.ID;
            }

            public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole)
            {
                MusicController controller = ((CommandMusic)Parent).Controller;
                ArgumentMap<bool, bool> args = Syntax.Parse<bool, bool>(argArray.ElementAtOrDefault(0), argArray.ElementAtOrDefault(1));

                if (!controller.Playing && !args.Arg2) throw new CommandException(this, Personality.Get("cmd.ori.music.err.notConnected"));
                if (controller.Encoding) throw new CommandException(this, Personality.Get("cmd.ori.music.err.busyEncoding"));
                if (DiscordClient.Current.DevMode && executor.GetPermissionLevel() < PermissionLevel.Operator) throw new CommandException(this, Personality.Get("cmd.ori.music.err.debugActive"));

                if (argArray.Length > 2)
                {
                    throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
                }

                if (args.Arg2)
                {
                    await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "Forcefully terminating music, even if the connection is not percieved as live.");
                    // await DiscordClient.Current.DisconnectFromVoiceAsync(true);
                    controller.ResetControllerState();
                }
                else
                {
                    await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "Alright! I'll stop the music " + (args.Arg1 ? "after this song is done playing." : "now."), mentions: AllowedMentions.Reply);
                    await ((CommandMusic)Parent).Controller.Stop(args.Arg1);
                }
            }
        }

        public class CommandMusicRestart : Command
        {
            public override string Name { get; } = "restart";
            public override string Description { get; } = "Votes to restart the music system.";
            public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<bool>("force").SetRequiredState(false);

            public CommandMusicRestart(BotContext ctx, Command parent) : base(ctx, parent)
            {
            }

            public override bool CanSeeHelpForAnyway { get; } = true;

            public override CommandUsagePacket CanRunCommand(Member member)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null) return new CommandUsagePacket(false, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (DiscordClient.Current.DevMode && member.GetPermissionLevel() < PermissionLevel.Operator) return new CommandUsagePacket(false, "The bot is currently in debug or developer mode, and as such, music cannot be played.");

                CommandUsagePacket baseUsage = base.CanRunCommand(member);
                if (!baseUsage.CanUse) return baseUsage;

                if (!controller.IsSystemEnabled) return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.systemIsOff"));

                if (member.CurrentVoiceChannel == controller.EffectiveMusicChannel) return CommandUsagePacket.Success;
                return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.notInMusicChannel"));
            }

            public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null) throw new CommandException(this, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (member.GetPermissionLevel() >= PermissionLevel.Operator) return channelUsedIn;
                if (channelUsedIn == executionContext.BotChannelID) return channelUsedIn;
                if (channelUsedIn == controller.MusicTextChannel.ID) return channelUsedIn;
                return controller.MusicTextChannel.ID;
            }

            public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole)
            {
                MusicController controller = ((CommandMusic)Parent).Controller;
                if (!controller.Playing) throw new CommandException(this, Personality.Get("cmd.ori.music.err.notConnected"));
                if (controller.Encoding) throw new CommandException(this, Personality.Get("cmd.ori.music.err.busyEncoding"));
                if (DiscordClient.Current.DevMode && executor.GetPermissionLevel() < PermissionLevel.Operator) throw new CommandException(this, Personality.Get("cmd.ori.music.err.debugActive"));

                if (argArray.Length > 1)
                {
                    throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
                }
                ArgumentMap<bool> args = Syntax.Parse<bool>(argArray.ElementAtOrDefault(0));
                if (args.Arg1)
                {
                    if (executor.GetPermissionLevel() >= PermissionLevel.Operator)
                    {
                        await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.restart"), mentions: AllowedMentions.Reply);
                        await ((CommandMusic)Parent).Controller.Reinitialize();
                        return;
                    }
                    else
                    {
                        throw new CommandException(this, Personality.Get("cmd.ori.music.err.invalidPerms", PermissionLevel.Operator.GetFullName()));
                    }
                }
                MusicVotingHelper voter = ((CommandMusic)Parent).Controller.VoteController;
                bool addedVote = voter.AddVoteToMusicRestart(executor);
                if (addedVote)
                {
                    await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.vote.restart.add"), mentions: AllowedMentions.Reply);
                }
                else
                {
                    await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.vote.restart.remove"), mentions: AllowedMentions.Reply);
                }
            }
        }

        public class CommandMusicVoteNext : Command
        {
            public override string Name { get; } = "votenext";
            public override string Description { get; } = "Votes for the next song or song category. A number of special keywords are usable for the `identity` argument, such as...\n\n• A **category**, which describes a game source: `bf`, `bfextra`, `wotw`, `wotwtrailers`, `trials`, `wotwextended`\n\n• A **title**: The title of a track (word for word) or the filename of the song without its extension.\n\n•A **spirit trial**: A query such as `Spirit Trial #1` (#1 through #8), or `Windswept Wastes Spirit Trial`";
            public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string, bool>("identity", "force").SetRequiredState(true, false);

            public CommandMusicVoteNext(BotContext ctx, Command parent) : base(ctx, parent)
            {
            }

            public override bool CanSeeHelpForAnyway { get; } = true;

            public override CommandUsagePacket CanRunCommand(Member member)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null) return new CommandUsagePacket(false, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (DiscordClient.Current.DevMode && member.GetPermissionLevel() < PermissionLevel.Operator) return new CommandUsagePacket(false, "The bot is currently in debug or developer mode, and as such, music cannot be played.");

                CommandUsagePacket baseUsage = base.CanRunCommand(member);
                if (!baseUsage.CanUse) return baseUsage;

                if (!controller.IsSystemEnabled) return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.systemIsOff"));

                if (member.CurrentVoiceChannel == controller.EffectiveMusicChannel) return CommandUsagePacket.Success;
                return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.notInMusicChannel"));
            }

            public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null) throw new CommandException(this, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (member.GetPermissionLevel() >= PermissionLevel.Operator) return channelUsedIn;
                if (channelUsedIn == executionContext.BotChannelID) return channelUsedIn;
                if (channelUsedIn == controller.MusicTextChannel.ID) return channelUsedIn;
                return controller.MusicTextChannel.ID;
            }

            public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole)
            {
                MusicController controller = ((CommandMusic)Parent).Controller;
                if (!controller.Playing) throw new CommandException(this, Personality.Get("cmd.ori.music.err.notConnected"));
                if (controller.Encoding) throw new CommandException(this, Personality.Get("cmd.ori.music.err.busyEncoding"));
                if (DiscordClient.Current.DevMode && executor.GetPermissionLevel() < PermissionLevel.Operator) throw new CommandException(this, Personality.Get("cmd.ori.music.err.debugActive"));

                if (argArray.Length == 0)
                {
                    throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
                }
                else if (argArray.Length > 2)
                {
                    throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
                }
                ArgumentMap<string, bool> args = Syntax.Parse<string, bool>(argArray.ElementAtOrDefault(0), argArray.ElementAtOrDefault(1));
                if (args.Arg2)
                {
                    if (executor.GetPermissionLevel() < PermissionLevel.Operator)
                    {
                        // special: handled ahead
                        throw new CommandException(this, Personality.Get("cmd.ori.music.err.invalidPerms", PermissionLevel.Operator.GetFullName()));
                    }
                }

                MusicVotingHelper voter = controller.VoteController;
                if (voter.TrackVotingLocked) throw new CommandException(this, Personality.Get("cmd.ori.music.err.vote.locked"));

                string targetArg = args.Arg1.ToLower();
                bool force = args.Arg2;
                MusicDirectory targetDir = null;
                foreach (MusicDirectory dir in controller.Pool.MusicDirectories)
                {
                    if (dir.Identities.Contains(targetArg))
                    {
                        targetDir = dir;
                        break;
                    }
                }

                if (targetDir != null)
                {
                    if (force)
                    {
                        // permission check done above.
                        MusicFile song = targetDir.GetRandomSong();
                        await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "The next song I'm playing is: " + song.Metadata.Title + " which was selected via getting a random song out of the category: " + targetDir.Name, mentions: AllowedMentions.Reply);
                        controller.ForceNext = song.File;
                        voter.TrackVotingLocked = true;
                        return;
                    }
                    else
                    {
                        bool voted = voter.AddVoteToNextCategory(executor, targetDir);
                        if (voted)
                        {
                            await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.vote.category.add", targetDir.Name), mentions: AllowedMentions.Reply);
                        }
                        else
                        {
                            await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.vote.category.remove", targetDir.Name), mentions: AllowedMentions.Reply);
                        }
                        return;
                    }
                }

                MusicFile targetFile = controller.Pool.TryGetFromTitleOrName(targetArg, out bool isLimitedTrack, out bool isMoreThanOne);
                if (isMoreThanOne)
                {
                    await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.err.vote.moreThanOneTrackFound"), mentions: AllowedMentions.Reply);
                    return;
                }
                if (isLimitedTrack)
                {
                    await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.err.vote.trackIsLimited"), mentions: AllowedMentions.Reply);
                    return;
                }

                if (targetFile != null)
                {
                    if (force)
                    {
                        await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "The next song I'm playing is: " + targetFile.Metadata.Title, mentions: AllowedMentions.Reply);
                        controller.ForceNext = targetFile.File;
                        voter.TrackVotingLocked = true;
                        return;
                    }
                    else
                    {
                        bool addedVote = voter.AddVoteToNextTrack(executor, targetFile);
                        if (addedVote)
                        {
                            await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.vote.track.add", targetFile.Metadata.Title), mentions: AllowedMentions.Reply);
                        }
                        else
                        {
                            await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.vote.track.remove", targetFile.Metadata.Title), mentions: AllowedMentions.Reply);
                        }
                        return;
                    }
                }

                if (targetArg.ToLower().StartsWith("spirit trial #") && targetArg.Length > 14)
                {
                    if (int.TryParse(targetArg[14..], out int trialNumber))
                    {
                        if (trialNumber < 1 || trialNumber > 8)
                        {
                            throw new CommandException(this, "Spirit Trial index is too small or big. It should be anywhere from 1 to 8.");
                        }

                        targetFile = null;
                        foreach (MusicFile file in controller.Pool[4].MusicFiles)
                        {
                            if (file.File.Name.StartsWith("Spirit Trial #" + trialNumber))
                            {
                                targetFile = file;
                                break;
                            }
                        }

                        if (targetFile == null)
                        {
                            throw new CommandException(this, "Spirit Trial index was within the proper range of 1 to 8, but I couldn't find a song from the given index!");
                        }
                        if (force)
                        {
                            await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "The next song I'm playing is: " + targetFile.Metadata.Title, mentions: AllowedMentions.Reply);
                            controller.ForceNext = targetFile.File;
                            voter.TrackVotingLocked = true;
                            return;
                        }
                        else
                        {
                            bool addedVote = voter.AddVoteToNextTrack(executor, targetFile);
                            if (addedVote)
                            {
                                await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.vote.track.add", targetFile.Metadata.Title), mentions: AllowedMentions.Reply);
                            }
                            else
                            {
                                await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.vote.track.remove", targetFile.Metadata.Title), mentions: AllowedMentions.Reply);
                            }
                            return;
                        }
                    }
                    else
                    {
                        throw new CommandException(this, "Spirit Trial index couldn't be turned into a number from 1 to 8.");
                    }
                }
                else if (targetArg.ToLower().EndsWith(" spirit trial"))
                {
                    targetFile = null;
                    string zoneName = targetArg.ToLower().Replace(" spirit trial", "");
                    foreach (MusicFile file in controller.Pool[4].MusicFiles)
                    {
                        string clippedName = file.File.Name.ToLower()[18..]; // This should cut off "Spirit Trial #x - "
                        clippedName = clippedName.Replace(file.File.Extension.ToLower(), "");
                        if (zoneName == clippedName)
                        {
                            targetFile = file;
                            break;
                        }
                    }
                    if (targetFile == null)
                    {
                        throw new CommandException(this, "Unable to find a Spirit Trial for the zone \"" + zoneName + "\"");
                    }
                    if (force)
                    {
                        await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "The next song I'm playing is the Spirit Trial for the zone \"" + zoneName + "\"", mentions: AllowedMentions.Reply);
                        controller.ForceNext = targetFile.File;
                        voter.TrackVotingLocked = true;
                        return;
                    }
                    else
                    {
                        bool addedVote = voter.AddVoteToNextTrack(executor, targetFile);
                        if (addedVote)
                        {
                            await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.vote.track.add", targetFile.Metadata.Title), mentions: AllowedMentions.Reply);
                        }
                        else
                        {
                            await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.vote.track.remove", targetFile.Metadata.Title), mentions: AllowedMentions.Reply);
                        }
                        return;
                    }
                }

                throw new CommandException(this, "I couldn't find a song from the query you gave me.");
            }
        }

        public class CommandMusicForceNext : Command
        {
            public override string Name { get; } = "forcenext";
            public override string Description { get; } = "Forces the next file that the music system will target, even if the file is not in the default music selection.";
            public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string>("filePath").SetRequiredState(true);
            public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;

            public CommandMusicForceNext(BotContext ctx, Command parent) : base(ctx, parent)
            {
            }

            public override bool CanSeeHelpForAnyway { get; } = true;

            public override CommandUsagePacket CanRunCommand(Member member)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null) return new CommandUsagePacket(false, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (DiscordClient.Current.DevMode && member.GetPermissionLevel() < PermissionLevel.Operator) return new CommandUsagePacket(false, "The bot is currently in debug or developer mode, and as such, music cannot be played.");

                CommandUsagePacket baseUsage = base.CanRunCommand(member);
                if (!baseUsage.CanUse) return baseUsage;

                if (!controller.IsSystemEnabled) return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.systemIsOff"));

                if (member.CurrentVoiceChannel == controller.EffectiveMusicChannel) return CommandUsagePacket.Success;
                return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.notInMusicChannel"));
            }

            public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null) throw new CommandException(this, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (member.GetPermissionLevel() >= PermissionLevel.Operator) return channelUsedIn;
                if (channelUsedIn == executionContext.BotChannelID) return channelUsedIn;
                if (channelUsedIn == controller.MusicTextChannel.ID) return channelUsedIn;
                return controller.MusicTextChannel.ID;
            }

            public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole)
            {
                MusicController controller = ((CommandMusic)Parent).Controller;
                if (!controller.Playing) throw new CommandException(this, Personality.Get("cmd.ori.music.err.notConnected"));
                if (controller.Encoding) throw new CommandException(this, Personality.Get("cmd.ori.music.err.busyEncoding"));

                if (argArray.Length == 0)
                {
                    throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
                }
                else if (argArray.Length > 1)
                {
                    throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
                }
                ArgumentMap<string> args = Syntax.Parse<string>(argArray[0]);
                FileInfo file = new FileInfo(args.Arg1);
                if (!file.Exists)
                {
                    throw new CommandException(this, Personality.Get("cmd.ori.music.err.fileDoesntExist"));
                }

                controller.ForceNext = file;
                await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.music.success.forceNext", file.FullName), mentions: AllowedMentions.Reply);
            }
        }

        public class CommandMusicVoteData : Command
        {
            public override string Name { get; } = "votedata";
            public override string Description { get; } = "Displays data about any ongoing votes.";
            public override ArgumentMapProvider Syntax { get; }
            public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;

            public CommandMusicVoteData(BotContext ctx, Command parent) : base(ctx, parent)
            {
            }

            public override bool CanSeeHelpForAnyway { get; } = true;

            public override CommandUsagePacket CanRunCommand(Member member)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null)
                    return new CommandUsagePacket(false, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (DiscordClient.Current.DevMode && member.GetPermissionLevel() < PermissionLevel.Operator)
                    return new CommandUsagePacket(false, "The bot is currently in debug or developer mode, and as such, music cannot be played.");

                CommandUsagePacket baseUsage = base.CanRunCommand(member);
                if (!baseUsage.CanUse)
                    return baseUsage;

                if (!controller.IsSystemEnabled)
                    return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.systemIsOff"));

                if (member.CurrentVoiceChannel == controller.EffectiveMusicChannel)
                    return CommandUsagePacket.Success;
                return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.notInMusicChannel"));
            }

            public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null)
                    throw new CommandException(this, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (member.GetPermissionLevel() >= PermissionLevel.Operator)
                    return channelUsedIn;
                if (channelUsedIn == executionContext.BotChannelID)
                    return channelUsedIn;
                if (channelUsedIn == controller.MusicTextChannel.ID)
                    return channelUsedIn;
                return controller.MusicTextChannel.ID;
            }

            public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole)
            {
                MusicController controller = ((CommandMusic)Parent).Controller;
                if (!controller.Playing)
                    throw new CommandException(this, Personality.Get("cmd.ori.music.err.notConnected"));
                if (controller.Encoding)
                    throw new CommandException(this, Personality.Get("cmd.ori.music.err.busyEncoding"));

                int count = controller.VoteController.LiveVotes.Count;
                if (count == 0)
                {
                    await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"There are currently {controller.EffectiveMusicChannel.ConnectedMembers.Count} people in the music channel including the bot.", null, AllowedMentions.Reply);
                }
                else if (count == 1)
                {
                    await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"There are currently {controller.EffectiveMusicChannel.ConnectedMembers.Count} people in the music channel including the bot.", controller.VoteController.LiveVotes[0].ToEmbed(), AllowedMentions.Reply);
                }
                else if (count > 1)
                {
                    await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"There are currently {controller.EffectiveMusicChannel.ConnectedMembers.Count} people in the music channel including the bot.", null, AllowedMentions.Reply);
                    foreach (Vote vote in controller.VoteController.LiveVotes)
                    {
                        await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, vote.ToEmbed(), AllowedMentions.Reply);
                    }
                }
            }
        }

        public class CommandMusicSystemData : Command
        {
            public override string Name { get; } = "systemdata";
            public override string Description { get; } = "Displays data about the system.";
            public override ArgumentMapProvider Syntax { get; }
            public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;

            public CommandMusicSystemData(BotContext ctx, Command parent) : base(ctx, parent)
            {
            }

            public override bool CanSeeHelpForAnyway { get; } = true;

            public override CommandUsagePacket CanRunCommand(Member member)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null)
                    return new CommandUsagePacket(false, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (DiscordClient.Current.DevMode && member.GetPermissionLevel() < PermissionLevel.Operator)
                    return new CommandUsagePacket(false, "The bot is currently in debug or developer mode, and as such, music cannot be played.");

                CommandUsagePacket baseUsage = base.CanRunCommand(member);
                if (!baseUsage.CanUse)
                    return baseUsage;

                if (!controller.IsSystemEnabled)
                    return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.systemIsOff"));

                if (member.GetPermissionLevel() < PermissionLevel.Operator)
                {
                    if (member.CurrentVoiceChannel == controller.EffectiveMusicChannel)
                        return CommandUsagePacket.Success;
                    return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.notInMusicChannel"));
                }
                return CommandUsagePacket.Success;
            }

            public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null)
                    throw new CommandException(this, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (member.GetPermissionLevel() >= PermissionLevel.Operator)
                    return channelUsedIn;
                if (channelUsedIn == executionContext.BotChannelID)
                    return channelUsedIn;
                if (channelUsedIn == controller.MusicTextChannel.ID)
                    return channelUsedIn;
                return controller.MusicTextChannel.ID;
            }

            public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole)
            {
                MusicController controller = ((CommandMusic)Parent).Controller;
                if (!controller.Playing)
                    throw new CommandException(this, Personality.Get("cmd.ori.music.err.notConnected"));
                if (controller.Encoding)
                    throw new CommandException(this, Personality.Get("cmd.ori.music.err.busyEncoding"));

                string response = $"There are currently {controller.EffectiveMusicChannel.ConnectedMembers.Count} people in the music channel including the bot.";
                response += $"\nThe server this was executed in currently has {executionContext.Server.VoiceStates.Count} VoiceState instances registered, which may or may not be connected. These states are:\n```\n";
                foreach (var state in executionContext.Server.VoiceStates)
                {
                    response += state.ToString();
                    response += "\n";
                }
                response += "```";
                await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, response, mentions: AllowedMentions.Reply);
            }
        }

        public class CommandMusicMigrate : Command
        {
            public override string Name { get; } = "migrate";
            public override string Description { get; } = "Moves the bot to a different channel until it disconnects via stop. Input a value of 0 to return to the default channel.";
            public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Snowflake>("channelId").SetRequiredState(true);
            public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;

            public CommandMusicMigrate(BotContext ctx, Command parent) : base(ctx, parent)
            {
            }

            public override CommandUsagePacket CanRunCommand(Member member)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null) return new CommandUsagePacket(false, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (DiscordClient.Current.DevMode && member.GetPermissionLevel() < PermissionLevel.Operator) return new CommandUsagePacket(false, "The bot is currently in debug or developer mode, and as such, music cannot be played.");

                CommandUsagePacket baseUsage = base.CanRunCommand(member);
                if (!baseUsage.CanUse) return baseUsage;

                if (!controller.IsSystemEnabled) return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.systemIsOff"));

                if (member.CurrentVoiceChannel == controller.EffectiveMusicChannel) return CommandUsagePacket.Success;
                return new CommandUsagePacket(false, Personality.Get("cmd.ori.music.err.notInMusicChannel"));
            }

            public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn)
            {
                MusicController controller = ((CommandMusic)Parent).PopulateControllerRef();
                if (controller == null) throw new CommandException(this, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
                if (member.GetPermissionLevel() >= PermissionLevel.Operator) return channelUsedIn;
                if (channelUsedIn == executionContext.BotChannelID) return channelUsedIn;
                if (channelUsedIn == controller.MusicTextChannel.ID) return channelUsedIn;
                return controller.MusicTextChannel.ID;
            }

            public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole)
            {
                MusicController controller = ((CommandMusic)Parent).Controller;
                if (controller.Encoding) throw new CommandException(this, Personality.Get("cmd.ori.music.err.busyEncoding"));

                if (argArray.Length == 0)
                {
                    throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
                }
                else if (argArray.Length > 1)
                {
                    throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
                }

                ArgumentMap<Snowflake> args = Syntax.Parse<Snowflake>(argArray.ElementAtOrDefault(0));

                if (!controller.Playing) throw new CommandException(this, Personality.Get("cmd.ori.music.err.notConnected"));
                VoiceChannel target = executionContext.Server.GetChannel<VoiceChannel>(args.Arg1);
                // target might be null and that's OK
                return controller.MigrateTo(target);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Data;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Music;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandNowPlaying : Command {
		public override string Name { get; } = "nowplaying";
		public override string Description { get; } = "When music is playing, this displays the song that is playing as well as its duration.";
		public override ArgumentMapProvider Syntax { get; }
		private MusicController Controller { get; set; }
		public override string[] Aliases { get; } = {
			"np"
		};
		public CommandNowPlaying(BotContext ctx) : base(ctx) { }

		public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn) {
			if (base.GetUseInChannel(executionContext, member, channelUsedIn) == channelUsedIn) return channelUsedIn;
			if (Controller != null) return Controller.MusicTextChannel.ID;
			return executionContext.BotChannelID;
		}

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (Controller == null) {
				CommandMusic musicCmd = executionContext.Commands.First(cmd => cmd is CommandMusic) as CommandMusic;
				Controller = musicCmd.PopulateControllerRef();
			}
			if (Controller == null) throw new CommandException(this, "This server is not set up for music transmission, or the necessary voice and text channels could not be found.");
			if (!Controller.Playing) throw new CommandException(this, Personality.Get("cmd.ori.music.err.nothingPlaying"));
			await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, Controller.GetFormattedNowPlaying(false), AllowedMentions.Reply);
		}
	}
}

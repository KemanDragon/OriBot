using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.Utility;
using OldOriBot.Utility.Arguments;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandUnmute : DeprecatedCommand {
		public override string Name { get; } = "unmute";
		public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
		public override bool RequiresContext { get; } = true;
		public CommandUnmute(CommandMute.CommandMuteRemove cmd) : base(cmd) { }
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.Interaction;

namespace OldOriBot.Data.Commands.ArgData {
	public class Channel : ICommandArg<Channel> {

		public GuildChannelBase Target { get; }

		public Channel() { }

		private Channel(string channel, BotContext context) {
			if (context == null) throw new InvalidOperationException("Cannot create a Channel arg without a BotContext.");
			if (Snowflake.TryParse(channel, out Snowflake channelId)) {
				Target = context.Server.GetChannel(channelId);
				return;
			}
			Match match = Regex.Match(channel, @"(<#){1}(\d+)(>){1}");
			if (match.Success) {
				string id = match.Groups[2].Value; // reminder to self: not zero-indexed (0 is the entire match itself, not one of the groups)
				if (Snowflake.TryParse(id, out channelId)) {
					Target = context.Server.GetChannel(channelId);
					return;
				} else {
					throw new FormatException("Given input not in the proper format. Expected a channel ID, or <#id>");
				}
			}
			throw new FormatException("Given input not in the proper format. Expected a channel ID, or <#id>");
		}

		public Channel From(string instance, object inContext) {
			return new Channel(instance, (BotContext)inContext);
		}

		object ICommandArg.From(string instance, object inContext) => ((ICommandArg<Channel>)this).From(instance, inContext);
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using OldOriBot.Interaction;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerOfTheMemes : PassiveHandler {
		public override string Name { get; } = "Handler of the Memes";
		public override string Description { get; } = "Boy, sometimes you just need a little less brain.";
		public HandlerOfTheMemes(BotContext ctx) : base(ctx) { }

		private DateTimeOffset LastPostedFoulPresence = default; // for the d u m m y ' s )yes spelled righT!!!)
		private DateTimeOffset LastPostedAboutPineapplePizza = default; // for the monsters

		public override async Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			string filtered = message.Content.ToLower().Replace(" ", "").Replace(".", "").Replace(",", "").Replace("?", "").Replace("!", "").Replace("_", "");
			if (filtered.Contains("stinkspirit")) {
				if (message.Channel.ID == 577567148973752320 || message.Channel.ID == 577567075938336790 || message.Channel.ID == 577567405723877416
				|| message.Channel.ID == 761124600741888020 || message.Channel.ID == 761124676910579742) {
					TimeSpan toPreventSpam = DateTimeOffset.UtcNow - LastPostedFoulPresence;
					if (toPreventSpam.TotalMinutes > 2) {
						LastPostedFoulPresence = DateTimeOffset.UtcNow;
						await ResponseUtil.RespondToAsync(message, HandlerLogger, "https://cdn.discordapp.com/attachments/625464757398405153/810458204114124800/its_not_stink_spirit_dummhy.png");
					}
				}
			} else if (filtered.Contains("pineapplepizza") || filtered.Contains("pineapplegoesonpizza")) {
				TimeSpan toPreventSpam = DateTimeOffset.UtcNow - LastPostedAboutPineapplePizza;
				if (toPreventSpam.TotalMinutes > 5) {
					await ResponseUtil.RespondToAsync(message, HandlerLogger, "I mean, what freak puts pineapples on a pizza, right? It ruins the pineapple AND the pizza!");
				}
			}
			return false;
		}
	}
}

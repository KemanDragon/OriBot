using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using OldOriBot.Interaction;
using OldOriBot.UserProfiles;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerProfileExperienceReward : PassiveHandler {
		public override string Name { get; } = "Profile Experience Reward Controller";
		public override string Description { get; } = "Responsible for awarding an experience point for every sent message.";
		public override bool RunOnCommands { get; } = true;
		public HandlerProfileExperienceReward(BotContext ctx) : base(ctx) { }
		public override Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			UserProfile profile = UserProfile.GetOrCreateProfileOf(executor);
			profile.Experience++;
			return HandlerDidNothingTask;
		}
	}
}

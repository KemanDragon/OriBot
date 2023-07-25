using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Interaction;
using OldOriBot.Utility.Arguments;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandReport : ProgressiveCommand {
		public override string Name { get; } = "report";
		public override string Description { get; } = "Reports a user to the moderation team.";
		public override string[] Prompts { get; } = new string[] {
			"Could you tell me who you want to report?",
			"Alright, now let me know what they did. Why do you want to report them?",
			"Okay. So to recap -- you want to report {0} ({1}), and the reason is:\n{2}\n\nIs this correct?"
		};

		public override ArgumentMapProvider[] Syntaxes { get; } = new ArgumentMapProvider[] {
			new ArgumentMapProvider<Person>("personToReport").SetRequiredState(true),
			new ArgumentMapProvider<string>("reasonForReport").SetRequiredState(true),
			new ArgumentMapProvider<bool>("confirmReport").SetRequiredState(true)
		};
		public CommandReport(BotContext inContext) : base(inContext) { }

		public override Task<Tracker> BeginExecutionAsync(Member executor, BotContext executionContext, Message originalMessage) {
			throw new NotImplementedException();
		}

		public class CommandReportTracker : Tracker {
			public CommandReportTracker(ProgressiveCommand source, ChannelBase channel) : base(source, channel) { }

			protected override Task ReshowCurrentStep(Message latestResponse) {
				throw new NotImplementedException();
			}

			protected override Task DisplayNextStep(Message latestResponse) {
				throw new NotImplementedException();
			}

			protected override Task Terminate(Message latestResponse) {
				throw new NotImplementedException();
			}

			protected override Task<TrackerActionResult> TryExecuteCurrentStep(Message latestResponse) {
				throw new NotImplementedException();
			}
		}
	}
}

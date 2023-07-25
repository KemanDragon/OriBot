using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Data.Persistence;
using OldOriBot.Interaction;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerAntiCopypasta : PassiveHandler {
		public override string Name { get; } = "Copypasta Recognition Systems";
		public override string Description { get; } = "Looks for patterns in common worm messages / spam chain messages, and deletes any messages of that nature, telling the poster that it is fake.";
		public override bool RunOnCommands { get; } = true;
		public DataPersistence AntiSpamPersistence => DataPersistence.GetPersistence(Context, "antispam.cfg"); // shared
		public bool IsEnabled => AntiSpamPersistence.TryGetType("BlockCopypasta", true);
		public HandlerAntiCopypasta(BotContext ctx) : base(ctx) { }

		private static readonly string[] KnownCopypastaStarts = new string[] {
			"EMERGENCY ALERT | Please read this carefully: A fair warning, Look out for a Discord user by the name of ",
		};

		private static readonly string[] KnownCopypastaEnds = new string[] {
			"SEND THIS TO ALL THE SERVERS YOU ARE IN."
		};

		private static readonly string[] Responses = new string[] {
			"This message is fake! Please do not propogate false messages through Discord servers. This message in particular has existed for several years and seems to pop up every once in a while. There is nothing to worry about, and exploits like this are completely impossible. Nobody can get your personal data simply by becoming your friend on Discord."
		};

		public override async Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			if (!IsEnabled) return false;

			string content = message.Content.ToLower();
			for (int idx = 0; idx < KnownCopypastaStarts.Length; idx++) {
				string start = KnownCopypastaStarts[idx].ToLower();
				string end = KnownCopypastaEnds[idx].ToLower();
				string response = Responses[idx].ToLower();

				bool hasStart = start != null && content.StartsWith(start);
				bool hasEnd = end != null && content.EndsWith(end);
				if (hasStart && hasEnd) {
					await message.DeleteAsync("This is a known spam message.");
					Message responseMessage = await executor.TrySendDMAsync(response);
					if (responseMessage == null) {
						// contengency plan
						await ResponseUtil.RespondToAsync(message, HandlerLogger, response, null, AllowedMentions.Reply, true, false, 30000);
					}
					return true;
				}
			}

			return false;
		}
	}
}

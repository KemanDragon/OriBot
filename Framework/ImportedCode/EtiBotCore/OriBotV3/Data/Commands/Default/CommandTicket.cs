using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using OldOriBot.CoreImplementation;
using OldOriBot.Interaction;
using OldOriBot.Interaction.CommandData;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OldOriBot.Data.Commands.Default {
	public class CommandTicket : Command {
		public const ulong MOD_THREAD_CTR_ID = 872548812525297695;

		public override string Name { get; } = "ticket";
		public override string Description { get; } = "Create a new help ticket";
		public override ArgumentMapProvider Syntax { get; }
		public override bool IsDMOnly { get; } = true;
		// public override PermissionLevel RequiredPermissionLevel => PermissionLevel.BotDeveloper;
		public CommandTicket() : base(null) { }

		/// <summary>
		/// Returns this user's existing thread, or null if it doesn't exist.
		/// </summary>
		/// <param name="executor"></param>
		/// <returns></returns>
		private Thread GetExistingThread(Member executor) {
			Thread thread = executor.Server.Threads.FirstOrDefault(thread => thread.Name.StartsWith(executor.ID.ToString()) && thread.ParentID == MOD_THREAD_CTR_ID && !thread.Deleted);
			return thread;
		}

		public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn) {
			return channelUsedIn;
		}

		public override async Task ExecuteCommandAsync(Member executor, BotContext _, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			BotContextOriTheGame executionContext = BotContextRegistry.GetContext<BotContextOriTheGame>();
			if (!executor.Roles.Contains(622258633303916564)) {
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "You need to be a member before you can send in tickets. If you are sending a ticket because you can't become a member, just message <@114163433980559366> directly and let him know that you can't get access to the server!");
				return;
			}

			Thread existing = GetExistingThread(executor);
			if (existing != null) {
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "You already have an open ticket! Head over to <#" + existing.ID + ">.");
				return;
			}
			Message msg = await originalMessage.ReplyAsync("Working...");

			TextChannel modThreadContainer = executionContext.Server.GetChannel<TextChannel>(MOD_THREAD_CTR_ID);
			existing = await modThreadContainer.CreateNewThread(executor.ID + " " + executor.FullName, ThreadArchiveDuration.Minutes4320, true, "Opened a ticket.");
			await existing.TryJoinAsync();
			await existing.SendMessageAsync("Let us know what's up, " + executor.Mention);
			await existing.TryAddMemberToThread(executor);
			await modThreadContainer.SendMessageAsync("<@&603306540438388756> A new ticket has been created: " + existing.Mention);
			// await modThreadContainer.SendMessageAsync("<@114163433980559366> A new ticket has been created: " + existing.Mention);

			msg.BeginChanges(true);
			msg.Content = "Done! Your ticket has been created at " + existing.Mention;
			await msg.ApplyChanges();
		}
	}
}

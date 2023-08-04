using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using OldOriBot.Data.Persistence;
using OldOriBot.Interaction;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerSecurityFilter : PassiveHandler {
		public override string Name { get; } = "Server Security Utility";
		public override string Description { get; } = "Assists in the identification and removal of known malicious actors and bots.";
		private DataPersistence OffenderList => DataPersistence.GetPersistence(Context, "known_offenders.cfg");
		private readonly WarningLevel AutoBanLevel = WarningLevel.MajorOffender;

		public HandlerSecurityFilter(BotContext ctx) : base(ctx) {
			DiscordClient.Current!.Events.MemberEvents.OnGuildMemberAdded += OnMemberAdded;
		}

		private (WarningLevel, string) GetOffenderData(Snowflake user) {
			string data = OffenderList.GetValue(user.ToString(), null, false, true);
			if (data != null) {
				string[] split = data.Split(new char[] { '|' }, 2);
				if (split.Length == 2 && int.TryParse(split[0], out int threatLevel)) {
					return ((WarningLevel)threatLevel, split[1]);
				}
				HandlerLogger.WriteWarning($"Attempt to load infraction data for user {user} failed due to incorrect formatting!");
				return (WarningLevel.NoThreat, null);
			}
			return (WarningLevel.NoThreat, null);
		}

		private async Task OnMemberAdded(Guild guild, Member member) {
			if (guild != Context?.Server) return;
			if (member.Username == "Discord Boosting Event") {
				await Context.EventLog.SendMessageAsync($"A potential spambot has joined! User is `Discord Boosting Event`. {member.ID} {member.Mention}");
			}

			(WarningLevel warningLevel, string reason) = GetOffenderData(member.ID);
			if (warningLevel > WarningLevel.NoThreat) {
				if (warningLevel >= AutoBanLevel) {
					await member.BanAsync(reason);
					await Context.EventLog.SendMessageAsync($"A user with documented negative behavior has joined. Alert level: {warningLevel} -- This puts them above the **automatic ban threshold**, and as such, action has been taken against them -- Documented Reason: {reason}");
				} else {
					await Context.EventLog.SendMessageAsync($"A user with documented negative behavior has joined. Alert level: {warningLevel} -- Documented Reason: {reason}");
				}
			}
		}

		public override Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) => HandlerDidNothingTask;

		public enum WarningLevel {

			/// <summary>
			/// No documented malicious activities.
			/// </summary>
			NoThreat = 0,

			/// <summary>
			/// Appears suspicious.
			/// </summary>
			HasSuspiciousBehavior = 1,

			/// <summary>
			/// A known offender, albeit only with minor problems.
			/// </summary>
			MinorOffender = 2,

			/// <summary>
			/// A known troublemaker.
			/// </summary>
			KnownOffender = 3,

			/// <summary>
			/// A serious troublemaker.
			/// </summary>
			MajorOffender = 4,

			/// <summary>
			/// Someone who is known to be outright destructive.
			/// </summary>
			ExtremeOffender = 5

		}
	}


}

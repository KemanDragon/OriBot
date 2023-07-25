using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using OldOriBot.Interaction;
using OldOriBot.Utility.Formatting;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerNicknameMarshaller : PassiveHandler { 

		public override string Name { get; } = "Nickname Compliance Marshaller";
		public override string Description { get; } = "Ensures nicknames follow server rules that prevent the use of certain special characters in names. Contains a complex system to remove fancy fonts.";

		private readonly List<Snowflake> SkipCheckMembers = new List<Snowflake>();

		public HandlerNicknameMarshaller(BotContext ctx) : base(ctx) {
			//DiscordClient.Current.Events.TypingEvents.OnTypingStarted += OnTypingStarted;
			DiscordClient.Current.Events.MemberEvents.OnGuildMemberUpdated += OnMemberUpdated;
			DiscordClient.Current.Events.MemberEvents.OnGuildMemberAdded += OnMemberAdded;
		}

		private async Task OnMemberAdded(Guild guild, Member mbr) {
			if (mbr.IsABot || mbr.IsDiscordSystem) return;
			if (guild != Context.Server) return;

			if (!FancyFontMap.NameHasFourOKCharsInARow(mbr.Username) && FancyFontMap.NameHasKnownUnwantedChars(mbr.Username)) {
				mbr.BeginChanges(true);
				mbr.Nickname = FancyFontMap.Convert(mbr.Username);
				await mbr.ApplyChanges("Username contained unwanted characters that makes them hard to ping. It has been replaced.");
			}
		}

		private async Task OnMemberUpdated(Guild guild, Member oldMember, Member mbr) {
			if (mbr.IsABot || mbr.IsDiscordSystem) return;
			if (guild != Context.Server) return;

			if (mbr.Nickname != null) {
				if (!FancyFontMap.NameHasFourOKCharsInARow(mbr.Nickname) && FancyFontMap.NameHasKnownUnwantedChars(mbr.Nickname)) {
					mbr.BeginChanges(true);
					mbr.Nickname = FancyFontMap.Convert(mbr.Nickname);
					await mbr.ApplyChanges("Nickname contained unwanted characters that makes them hard to ping. It has been replaced.");
					//SkipCheckMembers.Add(mbr.ID);
					//} else {
					//	SkipCheckMembers.Remove(mbr.ID);
				}
			} else {
				if (!FancyFontMap.NameHasFourOKCharsInARow(mbr.Username) && FancyFontMap.NameHasKnownUnwantedChars(mbr.Username)) {
					mbr.BeginChanges(true);
					mbr.Nickname = FancyFontMap.Convert(mbr.Username);
					await mbr.ApplyChanges("Username contained unwanted characters that makes them hard to ping. It has been replaced.");
					//SkipCheckMembers.Add(mbr.ID);
					//} else {
					//	SkipCheckMembers.Remove(mbr.ID);
				}
			}
		}

		public override Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) => HandlerDidNothingTask;
	}
}

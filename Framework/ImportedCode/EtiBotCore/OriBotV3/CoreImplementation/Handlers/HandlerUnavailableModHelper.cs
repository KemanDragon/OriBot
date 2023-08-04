using EtiBotCore.Client;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Interaction;
using OldOriBot.Utility.Responding;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerUnavailableModHelper : PassiveHandler {
		public override string Name { get; } = "Unavailable Moderator Helper System";
		public override string Description { get; } = "Looks for mods flagged as unavailable due to being on temporary leave, and notifies people who ping them.";
		public HandlerUnavailableModHelper(BotContext ctx) : base(ctx) { }


		const ulong UNAVAILABLE_MOD_ROLE_ID = 836933631950716938;

		public override async Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			if (executor.GetPermissionLevel() >= PermissionData.PermissionLevel.Operator) {
				if (!(DiscordClient.Current.DevMode && executor.ID == 114163433980559366)) {
					return false;
				}
			}
			
			if (message.Channel == executionContext.GetPassiveHandlerInstance<HandlerArtPinSystem>().ArtChannel) return false;
			
			foreach (User usr in message.Mentions) {
				Member mbr = await usr.InServerAsync(executionContext.Server);
				if (mbr == null) continue;
				if (mbr.Roles.Contains(UNAVAILABLE_MOD_ROLE_ID) && mbr.GetPermissionLevel() >= PermissionData.PermissionLevel.Operator) {
					// Unavailable mod role
					await ResponseUtil.RespondToAsync(message, HandlerLogger, $"Hey! While I am not set up to try to get context on your message (so this message could be entirely out of context and flat out wrong), I see you pinging {mbr.Mention}. If, by chance, this mention is being done for moderation purposes, they are currently unavailable for this purpose (note the <@&{UNAVAILABLE_MOD_ROLE_ID}> role) and so you will need to ping someone else.\n\nThis message will delete itself in 10 seconds.", mentions: AllowedMentions.Reply, deleteAfterMS: 10000);
					return false;
				}
			}
			return false;
		}
	}
}

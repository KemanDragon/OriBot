using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using OldOriBot.Interaction;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerGrantImageRole : PassiveHandler {
		public override string Name { get; } = "Image Role Granting System";
		public override string Description { get; } = "Grants the Images role to those who have been here long enough.";
		public HandlerGrantImageRole(BotContext ctx) : base(ctx) { }

		private Role ImagesRole { get; set; }

		public override async Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			if (ImagesRole == null) {
				ImagesRole = ((IEnumerable<Role>)executionContext.Server.Roles).FirstOrDefault(role => role.Name == "Images");
			}
			if (ImagesRole == null) return false;

			if (executor.Roles.Contains(ImagesRole)) return false;
			if ((DateTimeOffset.UtcNow - executor.JoinedAt).Days >= 2) {
				executor.BeginChanges(true);
				executor.Roles.Add(ImagesRole);
				await executor.ApplyChanges("Granted images role due to being present for 2 days.");
			}
			return false;
		}
	}
}

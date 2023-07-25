using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Utility.Extension;

namespace EtiBotCore.Payloads.Events.Intents.GuildBans {

	/// <summary>
	/// Fired when a member is unbanned from a guild.
	/// </summary>
	internal class GuildBanRemoveEvent : GuildGenericBanEvent, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID);
			var user = DiscordObjects.Universal.User.EventGetOrCreate(User);
			await fromClient.Events.BanEvents.OnMemberUnbanned.Invoke(guild, user);
		}
	}
}

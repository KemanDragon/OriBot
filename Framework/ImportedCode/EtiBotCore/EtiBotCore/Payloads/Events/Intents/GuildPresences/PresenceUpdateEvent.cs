using EtiBotCore.Client;
using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.PayloadObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Intents.GuildPresences {

	/// <summary>
	/// Fired when the presence of a user changes.<para/>
	/// This event is unique in that it may not have any given field populated. Additionally, the user may be partial (only the ID field set)
	/// </summary>
	internal class PresenceUpdateEvent : Presence, IEvent {

		public async Task Execute(DiscordClient fromClient) {
			if (GuildID != null && User != null) {
				var server = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID!.Value, true);
				var mbr = await server.GetMemberAsync(User!.UserID);
				if (mbr != null) {
					var oldPresence = mbr.Presence.Clone();
					await mbr.Update(this, false);
					await fromClient.Events.PresenceEvents.OnPresenceUpdated.Invoke(oldPresence, mbr.Presence);
				}
			}

			await fromClient.Events.PresenceEvents.OnPresenceUpdated.Invoke(null, new DiscordObjects.Guilds.MemberData.Presence(this));
		}
	}
}

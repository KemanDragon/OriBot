using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.PayloadObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {
	internal class ThreadDeleteEvent : Channel, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			Snowflake serverId = GuildID!.Value;
			Snowflake parentChannelId = ParentID!.Value;
			// Try this:
			var existingThread = DiscordObjects.Base.GuildChannelBase.GetFromCache<DiscordObjects.Guilds.Thread>(ID);
			if (existingThread != null) existingThread.Deleted = true;

			await fromClient.Events.GuildEvents.OnThreadDeleted.Invoke(existingThread, ID, serverId, parentChannelId, Type);
		}
	}
}

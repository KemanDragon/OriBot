using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events.Intents.Guilds {
	internal class ThreadMembersUpdateEvent : ThreadMembersUpdatePacket, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			var server = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(ServerID);
			var thread = DiscordObjects.Base.GuildChannelBase.GetFromCache<DiscordObjects.Guilds.Thread>(ID);
			var members = new List<DiscordObjects.Guilds.Member>();

			if (AddedMembers != null) {
				foreach (ThreadMember mbr in AddedMembers) {
					var serverMbr = (await server.GetMemberAsync(mbr.UserID!.Value))!;
					members.Add(serverMbr);
					if (thread != null && !thread.IgnoresNetworkUpdates) {
						if (!thread._Members.Contains(serverMbr)) {
							thread._Members.Add(serverMbr);
						}
					}
				}
			}
			if (thread != null && RemovedMemberIDs != null && !thread.IgnoresNetworkUpdates) {
				foreach (Snowflake id in RemovedMemberIDs) {
					var target = thread._Members.FirstOrDefault(mbr => mbr.ID == id);
					if (target != null) {
						thread._Members.Remove(target);
					}
				}
			}

			await fromClient.Events.GuildEvents.OnThreadMembersUpdated.Invoke(server, thread, members.ToArray(), RemovedMemberIDs.ToType<ulong, Snowflake>().ToArray());
		}
	}
}

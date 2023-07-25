using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.GuildMembers {

	/// <summary>
	/// Fired when a member leaves a guild.
	/// </summary>
	class GuildMemberRemoveEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the server that this member was removed from.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong GuildID { get; set; }

		/// <summary>
		/// The user that was removed.
		/// </summary>
		[JsonProperty("user"), JsonRequired]
		public User User { get; set; } = new User();

		public async Task Execute(DiscordClient fromClient) {
			var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID);
			var member = DiscordObjects.Guilds.Member.EventGetOrCreate(User, guild);
			member.Deleted = true;

			await fromClient.Events.MemberEvents.OnGuildMemberRemoved.Invoke(guild, member);
		}
	}
}

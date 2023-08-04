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
	/// Fired when someone joins a guild. Identical to a standard <see cref="Member"/> but with an extra field, <see cref="GuildID"/>
	/// </summary>
	internal class GuildMemberAddEvent : Member, IEvent {

		/// <summary>
		/// The ID of the server that this member was added to.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong GuildID { get; set; }

		public async Task Execute(DiscordClient fromClient) {
			var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID);
			var member = DiscordObjects.Guilds.Member.EventGetOrCreate(User!, guild);

			// in case it's a rejoin
			member.IsShallow = false; // This is important either way ^
			member.Deleted = false;
			member.Roles.ClearInternally();
			await member.UpdateFromObject(this, false);

			await fromClient.Events.MemberEvents.OnGuildMemberAdded.Invoke(guild, member);
		}
	}
}

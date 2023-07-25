using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.GuildMembers {

	/// <summary>
	/// Fired when a member changes in a guild.
	/// </summary>
	class GuildMemberUpdateEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the server that this change occurred in.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong GuildID { get; set; }

		/// <summary>
		/// The IDs of the roles this user now has.
		/// </summary>
		[JsonProperty("roles"), JsonRequired]
		public ulong[] Roles { get; set; } = new ulong[0];

		/// <summary>
		/// The user that was changed.
		/// </summary>
		[JsonProperty("user"), JsonRequired]
		public User User { get; set; } = new User();

		/// <summary>
		/// The user's nickname, or <see langword="null"/> if they don't have one.
		/// </summary>
		[JsonProperty("nick", NullValueHandling = NullValueHandling.Ignore)]
		public string? Nickname { get; set; }

		/// <summary>
		/// When this user joined the server.
		/// </summary>
		[JsonProperty("joined_at"), JsonConverter(typeof(TimestampConverter))]
		public ISO8601 JoinedAt { get; set; }

		/// <summary>
		/// When this user started boosting the server, or <see langword="null"/> if they are not boosting it.
		/// </summary>
		[JsonProperty("premium_since", NullValueHandling = NullValueHandling.Ignore), JsonConverter(typeof(TimestampConverter))]
		public ISO8601? PremiumSince { get; set; }

		public async Task Execute(DiscordClient fromClient) {
			var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID);

			int tries = 0;
			while (DiscordObjects.Guilds.Member.InstantiatedMembers.TryGetValue(guild.ID, out var _) == false
				&& DiscordObjects.Guilds.Member.InstantiatedMembers[guild.ID].TryGetValue(User.UserID, out var _) == false && tries < 10) {
				tries++;
				await Task.Delay(500);
			}
			if (tries == 10) {
				DiscordClient.Log.WriteCritical("Member update received before the actual member was received, and the member still wasn't received after waiting a while. I'm dropping this event.", EtiLogger.Logging.LogLevel.Trace);
				return;
			}

			var member = DiscordObjects.Guilds.Member.EventGetOrCreate(User, guild);
			var oldMember = (DiscordObjects.Guilds.Member)member.MemberwiseClone();
			await member.UpdateFromObject(this, false); // Intentionally false

			/*
			if (!guild.Members.Contains(member)) {
				var mbrs = guild.Members.ToList();
				mbrs.Add(member);
				guild.Members = mbrs;
			}
			*/
			
			await fromClient.Events.MemberEvents.OnGuildMemberUpdated.Invoke(guild, oldMember, member);
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.GuildInvites {

	/// <summary>
	/// Fired when an invite is deleted from a guild.
	/// </summary>
	internal class InviteDeleteEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The channel that the invite leads to, or <see langword="null"/> if this is not applicable.
		/// </summary>
		[JsonProperty("channel_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong ChannelID { get; set; }

		/// <summary>
		/// The server that the invite leads to, or <see langword="null"/> if this is not applicable.
		/// </summary>
		[JsonProperty("guild_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? GuildID { get; set; }

		/// <summary>
		/// The unique code of the invite.
		/// </summary>
		[JsonProperty("code"), JsonRequired]
		public string InviteCode { get; set; } = string.Empty;

		public virtual async Task Execute(DiscordClient fromClient) {
			await fromClient.Events.InviteEvents.OnInviteDeleted.Invoke(GuildID, ChannelID, InviteCode);
		}
	}
}

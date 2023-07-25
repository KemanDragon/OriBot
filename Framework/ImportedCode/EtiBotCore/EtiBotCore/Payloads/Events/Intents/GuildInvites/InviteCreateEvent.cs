using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.PayloadObjects;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.GuildInvites {

	/// <summary>
	/// Fires when a guild invite is created.
	/// </summary>
	internal class InviteCreateEvent : InviteDeleteEvent, IEvent {

		/// <summary>
		/// When this invite was created.
		/// </summary>
		[JsonProperty("created_at")]
		public ISO8601 CreatedAt { get; set; }

		/// <summary>
		/// The user who created the invite.
		/// </summary>
		[JsonProperty("inviter", NullValueHandling = NullValueHandling.Ignore)]
		public User? Inviter { get; set; }

		/// <summary>
		/// The time that the invite is valid for in seconds.
		/// </summary>
		[JsonProperty("max_age")]
		public int MaxAge { get; set; }

		/// <summary>
		/// The maximum amount of times the invite can be used.
		/// </summary>
		[JsonProperty("max_uses")]
		public int MaxUses { get; set; }

		/// <summary>
		/// The user this invite was sent to (partial), or <see langword="null"/> if this invite was not created for someone in specific.
		/// </summary>
		[JsonProperty("target_user", NullValueHandling = NullValueHandling.Ignore)]
		public User? TargetUser { get; set; }

		/// <summary>
		/// The target user type, or <see langword="null"/> if <see cref="TargetUser"/> is <see langword="null"/>.<para/>
		/// Right now, the only user type is 1.
		/// </summary>
		[JsonProperty("target_user_type", NullValueHandling = NullValueHandling.Ignore)]
		public int? TargetUserType { get; set; }

		/// <summary>
		/// Whether or not this invite grants a temporary membership - If the user logs off and has no roles, they will be removed from the server.
		/// </summary>
		[JsonProperty("temporary")]
		public bool Temporary { get; set; }

		/// <summary>
		/// The amount of times this invite has been used.<para/>
		/// <strong>This is always 0.</strong>
		/// </summary>
		[JsonProperty("uses")]
		public int Uses { get; set; }

		public override async Task Execute(DiscordClient fromClient) {
			Invite inv = new Invite() {
				CreatedAt = CreatedAt.DateTime,
				Server = GuildID,
				Channel = ChannelID,
				Inviter = Inviter != null ? new DiscordObjects.Universal.PartialUser(Inviter.UserID, Inviter.Username, Inviter.Discriminator, Inviter.AvatarHash) : null,
				MaxAge = MaxAge,
				MaxUses = MaxUses,
				TargetUser = TargetUser != null ? new DiscordObjects.Universal.PartialUser(TargetUser.UserID, TargetUser.Username, TargetUser.Discriminator, TargetUser.AvatarHash) : null,
				Temporary = Temporary,
				Code = InviteCode
			};
			await fromClient.Events.InviteEvents.OnInviteCreated.Invoke(inv);
		}
	}
}

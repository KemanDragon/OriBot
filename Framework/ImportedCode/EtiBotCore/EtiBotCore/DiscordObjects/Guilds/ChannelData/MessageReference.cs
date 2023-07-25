using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using Newtonsoft.Json;

namespace EtiBotCore.DiscordObjects.Guilds.ChannelData {

	/// <summary>
	/// A reference to another message.
	/// </summary>
	
	public class MessageReference {

		/// <summary>
		/// The ID of the original message, if applicable.
		/// </summary>
		[JsonProperty("message_id")]
		public Snowflake? MessageID { get; internal set; }

		/// <summary>
		/// The ID of the channel that this message came from.
		/// </summary>
		[JsonProperty("channel_id")]
		public Snowflake? ChannelID { get; internal set; }

		/// <summary>
		/// The server that this message came from, or <see langword="null"/> if there is no associated server.
		/// </summary>
		[JsonProperty("guild_id")]
		public Snowflake? GuildID { get; internal set; }

		internal static MessageReference CreateFromPayload(Payloads.PayloadObjects.MessageReference reference) {
			return new MessageReference {
				MessageID = reference.MessageID,
				ChannelID = reference.ChannelID,
				GuildID = reference.GuildID
			};
		}

		internal MessageReference() { }
		internal MessageReference(MessageReference other) {
			MessageID = other.MessageID;
			ChannelID = other.ChannelID;
			GuildID = other.GuildID;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return $"MessageReference[MessageID={MessageID}, ChannelID={ChannelID}, GuildID={GuildID}]";
		}
	}
}

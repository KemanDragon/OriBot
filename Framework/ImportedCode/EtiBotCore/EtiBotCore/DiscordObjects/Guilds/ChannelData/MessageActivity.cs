using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads.Data;

namespace EtiBotCore.DiscordObjects.Guilds.ChannelData {

	/// <summary>
	/// Activity data in a message, usually for invites e.g. game invites or spotify invites.
	/// </summary>
	
	public class MessageActivity {

		/// <summary>
		/// The type of activity that this message performs.
		/// </summary>
		public MessageActivityType Type { get; internal set; }

		/// <summary>
		/// The ID of the party.
		/// </summary>
		public string? PartyID { get; internal set; }

		/// <summary>
		/// Creates a <see cref="MessageActivity"/> from the given payload.
		/// </summary>
		/// <param name="pl"></param>
		/// <returns></returns>
		internal static MessageActivity? CreateFromPayload(Payloads.PayloadObjects.MessageActivity? pl) {
			if (pl == null) return null;
			return new MessageActivity {
				Type = pl.Type,
				PartyID = pl.PartyID
			};
		}

		internal MessageActivity() { }
		internal MessageActivity(MessageActivity other) {
			Type = other.Type;
			PartyID = other.PartyID;
		}

	}
}

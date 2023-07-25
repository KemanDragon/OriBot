using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.DiscordObjects.Universal {

	/// <summary>
	/// A reaction on a message exclusively by emoji and amount.
	/// </summary>
	public class Reaction {

		/// <summary>
		/// The amount of times this reaction has been added.
		/// </summary>
		public int Count { get; internal set; }

		/// <summary>
		/// Whether or not this bot has reacted with this emoji.
		/// </summary>
		public bool SelfIncluded { get; internal set; }

		/// <summary>
		/// A partial <see cref="Emoji"/> object.
		/// </summary>
		public Emoji Emoji { get; internal set; }

		/// <summary>
		/// Only to be used when synchronizing reaction containers.
		/// </summary>
		internal Reaction(Emoji emoji) { Emoji = emoji; }

		internal Reaction(Payloads.PayloadObjects.Reaction plReaction) {
			Count = plReaction.Count;
			SelfIncluded = plReaction.Me;
			if (plReaction.Emoji.ID == null) {
				// This is a unicode emoji. Name will not be null here.
				Emoji = Emoji.GetOrCreate(plReaction.Emoji.Name!);
			} else {
				Emoji = CustomEmoji.GetOrCreate(plReaction.Emoji);
			}
		}

		internal static Reaction? CreateFromPayload(Payloads.PayloadObjects.Reaction? plRxn) {
			if (plRxn == null) return null;
			return new Reaction(plRxn);
		}

		/// <summary>
		/// Returns a shallow-copy of this <see cref="Reaction"/> (it doesn't need a deep copy)
		/// </summary>
		/// <returns></returns>
		internal Reaction Clone() {
			return (Reaction)MemberwiseClone();
		}
	}
}

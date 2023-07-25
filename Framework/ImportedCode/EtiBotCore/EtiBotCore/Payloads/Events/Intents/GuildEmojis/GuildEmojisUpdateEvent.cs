using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.Events.Intents.GuildEmojis {

	/// <summary>
	/// Fired when the emojis in a guild are changed, either added, removed, or renamed.
	/// </summary>
	internal class GuildEmojisUpdateEvent : PayloadDataObject, IEvent {

		/// <summary>
		/// The ID of the server that this emoji change occurred in.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong GuildID { get; set; }

		/// <summary>
		/// The emojis in this server.
		/// </summary>
		[JsonProperty("emojis"), JsonRequired]
		public PayloadObjects.Emoji[] Emojis { get; set; } = new PayloadObjects.Emoji[0];

		public async Task Execute(DiscordClient fromClient) {
			var guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID);
			List<CustomEmoji> emojis = new List<CustomEmoji>();
			foreach (PayloadObjects.Emoji emoji in Emojis) {
				// This will always be custom, since it's server emojis
				emojis.Add(CustomEmoji.GetOrCreate(emoji));
			}
			var old = guild.Emojis.ToArray();
			var gEmojis = guild.Emojis.ToList();
			gEmojis.AddRange(emojis);
			guild.Emojis = gEmojis.AsReadOnly();
			await fromClient.Events.EmojiEvents.OnEmojisUpdated.Invoke(guild, old, emojis.ToArray());
		}
	}
}

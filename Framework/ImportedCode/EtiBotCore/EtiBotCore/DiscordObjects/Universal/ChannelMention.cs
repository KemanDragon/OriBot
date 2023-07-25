using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;

namespace EtiBotCore.DiscordObjects.Universal {

	/// <summary>
	/// A reference to a channel included in a message.
	/// </summary>
	
	public class ChannelMention {

		/// <summary>
		/// The channel this refers to.
		/// </summary>
		public GuildChannelBase Channel { get; internal set; }

		/// <summary>
		/// The type of channel that this is.
		/// </summary>
		public ChannelType Type { get; internal set; }

		private ChannelMention(GuildChannelBase channel, ChannelType type) {
			Channel = channel;
			Type = type;
		}

		/// <summary>
		/// Creates a new <see cref="ChannelMention"/> from the payload variant. This may download the guild and its channels.
		/// </summary>
		/// <param name="payload"></param>
		/// <returns></returns>
		internal static async Task<ChannelMention> CreateFromPayloadAsync(Payloads.PayloadObjects.ChannelMention? payload) {
			if (payload == null) throw new ArgumentNullException(nameof(payload));
			Guild server = await Guild.GetOrDownloadAsync(payload.GuildID);
			if (server.Channels.Count == 0) await server.ForcefullyAcquireChannelsAsync();
			return new ChannelMention(server.GetChannel(payload.ID)!, payload.Type);
		}

	}
}

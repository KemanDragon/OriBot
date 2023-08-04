using EtiBotCore.Utility.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// Represents a type of Discord Channel.
	/// </summary>
	public enum ChannelType {

		/// <summary>
		/// A generic text channel in a server.
		/// </summary>
		Text = 0,

		/// <summary>
		/// A DM channel with this bot.
		/// </summary>
		DM = 1,

		/// <summary>
		/// A voice channel in a server.
		/// </summary>
		Voice = 2,

		/// <summary>
		/// A group DM including this bot.
		/// </summary>
		GroupDM = 3,

		/// <summary>
		/// A channel category.
		/// </summary>
		Category = 4,

		/// <summary>
		/// A news channel, which is a channel that can be followed.
		/// </summary>
		News = 5,

		/// <summary>
		/// A store channel, which developers can use to sell their game.
		/// </summary>
		Store = 6,

		/// <summary>
		/// A thread that is part of a news channel (a child of a channel of type <see cref="News"/>)
		/// </summary>
		NewsThread = 10,

		/// <summary>
		/// A publicly-accessible thread that is part of a text channel (a child of a channel of type <see cref="Text"/>)
		/// </summary>
		PublicThread = 11,

		/// <summary>
		/// A privately-accessible thread that is part of a text channel (a child of a channel of type <see cref="Text"/>)
		/// </summary>
		PrivateThread = 12,

		/// <summary>
		/// A stage channel for hosting large events.
		/// </summary>
		StageVoice = 13
	}
}

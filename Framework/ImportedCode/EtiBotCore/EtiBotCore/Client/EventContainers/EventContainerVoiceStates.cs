#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Universal;
using SignalCore;

namespace EtiBotCore.Client.EventContainers {

	/// <summary>
	/// A container for events pertaining to the voice chat presence of a member or user.
	/// </summary>
	public class EventContainerVoiceStates {

		internal EventContainerVoiceStates() { }

		/// <summary>
		/// An event that fires when the voice state of a member changes. The input channelId could be <see cref="Snowflake.Invalid"/> under certain events, such as if they left the channel.
		/// </summary>
		/// <remarks>
		/// <strong>Parameters:</strong> <c>voiceStateBefore, voiceStateAfter, serverId, channelId</c>
		/// </remarks>
		public Signal<VoiceState, VoiceState, Snowflake?, Snowflake> OnVoiceStateChanged = new Signal<VoiceState, VoiceState, Snowflake?, Snowflake>();

	}
}

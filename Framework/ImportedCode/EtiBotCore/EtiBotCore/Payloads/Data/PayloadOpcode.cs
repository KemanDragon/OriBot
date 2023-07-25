using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// An opcode for a payload to or from Discord.
	/// </summary>
	public enum PayloadOpcode {

		/// <summary>
		/// A faulty <see cref="PayloadOpcode"/> that will never be sent by Discord.
		/// </summary>
		NULL = -1,

		/// <summary>
		/// An event was dispatched.
		/// <para/>
		/// <strong>This operation is:</strong> Received
		/// </summary>
		Dispatch = 0,

		/// <summary>
		/// Fired periodically by the client to keep the connection alive.
		/// <para/>
		/// <strong>This operation is:</strong> Sent &amp; Received
		/// </summary>
		Heartbeat = 1,

		/// <summary>
		/// <strong>This operation is only used in voice connections.</strong><para/>
		/// Select the protocol used to connect to voice.
		/// </summary>
		SelectProtocol = 1,

		/// <summary>
		/// Starts a new session during the initial handshake.
		/// <para/>
		/// <strong>This operation is:</strong> Sent
		/// </summary>
		Identify = 2,

		/// <summary>
		/// Update the client's presence.
		/// <para/>
		/// <strong>This operation is:</strong> Sent
		/// </summary>
		PresenceUpdate = 3,

		/// <summary>
		/// <strong>This operation is only used in voice connections.</strong><para/>
		/// A voice heartbeat.
		/// </summary>
		VoiceHeartbeat = 3,

		/// <summary>
		/// Used to join/leave or move between voice channels.
		/// <para/>
		/// <strong>This operation is:</strong> Sent
		/// </summary>
		VoiceStateUpdate = 4,

		/// <summary>
		/// <strong>Only used in voice connections.</strong> This payload contains information about speaking.
		/// </summary>
		Speaking = 5,

		/// <summary>
		/// Resume a previous session that was disconnected.
		/// <para/>
		/// <strong>This operation is:</strong> Sent
		/// </summary>
		Resume = 6,

		/// <summary>
		/// <strong>Only used in voice connections.</strong> This is the opcode for an acknowledged heartbeat.
		/// <para/>
		/// <strong>This operation is:</strong> Received
		/// </summary>
		VoiceHeartbeatAcknowledged = 6,

		/// <summary>
		/// You should attempt to reconnect and resume immediately.
		/// <para/>
		/// <strong>This operation is:</strong> Received
		/// </summary>
		Reconnect = 7,

		/// <summary>
		/// <strong>Only used in voice connections.</strong> You are attempting to resume an existing voice connection that has failed.
		/// <para/>
		/// <strong>This operation is:</strong> Sent
		/// </summary>
		VoiceResume = 7,

		/// <summary>
		/// Request information about offline guild members in a large guild.
		/// <para/>
		/// <strong>This operation is:</strong> Sent
		/// </summary>
		RequestGuildMembers = 8,

		/// <summary>
		/// <strong>Only used in voice connections.</strong> Equivalent to <see cref="Hello"/> for voice connections.
		/// </summary>
		VoiceHello = 8,

		/// <summary>
		/// The session has been invalidated. You should reconnect and identify/resume accordingly.
		/// <para/>
		/// <strong>This operation is:</strong> Received
		/// </summary>
		InvalidSession = 9,

		/// <summary>
		/// <strong>Only used in voice connections.</strong> The successful response to <see cref="VoiceResume"/>.
		/// <para/>
		/// <strong>This operation is:</strong> Received
		/// </summary>
		VoiceResumed = 9,

		/// <summary>
		/// Sent immediately after connecting, contains the heartbeat_interval to use.
		/// <para/>
		/// <strong>This operation is:</strong> Received
		/// </summary>
		Hello = 10,

		/// <summary>
		/// Sent in response to receiving a heartbeat to acknowledge that it has been received.
		/// <para/>
		/// <strong>This operation is:</strong> Received
		/// </summary>
		HeartbeatAcknowledged = 11

	}
}

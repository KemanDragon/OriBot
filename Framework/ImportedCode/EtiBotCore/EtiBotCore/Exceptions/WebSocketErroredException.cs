using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Exceptions {

	/// <summary>
	/// An exception raised when the websocket errors.
	/// </summary>
	public class WebSocketErroredException : Exception {

		private static readonly Dictionary<DiscordGatewayEventCode, string> DiscordErrorMessages = new Dictionary<DiscordGatewayEventCode, string>() {
			[DiscordGatewayEventCode.UnknownError] = "We're not sure what went wrong. Try reconnecting?",
			[DiscordGatewayEventCode.UnknownOpcode] = "You sent an invalid Gateway opcode or an invalid payload for an opcode. Don't do that!",
			[DiscordGatewayEventCode.DecodeError] = "You sent an invalid payload to us. Don't do that!",
			[DiscordGatewayEventCode.NotAuthenticated] = "You sent us a payload prior to identifying.",
			[DiscordGatewayEventCode.AuthenticationFailed] = "The account token sent with your identify payload is incorrect.",
			[DiscordGatewayEventCode.AlreadyAuthenticated] = "You sent more than one identify payload. Don't do that!",
			[DiscordGatewayEventCode.SessionNoLongerValid] = "Your voice session is no longer valid.",
			[DiscordGatewayEventCode.InvalidResumeSequence] = "The sequence sent when resuming the session was invalid. Reconnect and start a new session.",
			[DiscordGatewayEventCode.RateLimited] = "Woah nelly! You're sending payloads to us too quickly. Slow it down! You will be disconnected on receiving this.",
			[DiscordGatewayEventCode.TimedOut] = "Your session timed out. Reconnect and start a new one.",
			[DiscordGatewayEventCode.InvalidShard] = "You sent us an invalid shard when identifying.",
			[DiscordGatewayEventCode.ShardingIsRequiredOrServerNotFound] = "The session would have handled too many guilds - you are required to shard your connection in order to connect, or for voice connections, we can't find the server you're trying to connect to.",
			[DiscordGatewayEventCode.InvalidAPIVersionOrUnknownProtocol] = "You sent an invalid version for the gateway, or an invalid protocol for a voice connection.",
			[DiscordGatewayEventCode.InvalidIntent] = "You sent an invalid intent for a Gateway Intent. You may have incorrectly calculated the bitwise value.",
			[DiscordGatewayEventCode.NotAuthorizedToUseIntentOrVoiceDisconnected] = "You sent a disallowed intent for a Gateway Intent. You may have tried to specify an intent that you have not enabled or are not whitelisted for, or for voice connections, the channel was deleted, you were kicked, the voice server changed, or the main gateway session was dropped. Do not reconnect.",
			[DiscordGatewayEventCode.VoiceServerCrashed] = "The server crashed. Our bad! Try resuming.",
			[DiscordGatewayEventCode.UnknownEncryptionMode] = "We didn't recognize your encryption."
		};

		/// <summary>
		/// The code of this error, or -1 if one was not provided when the exception was thrown.
		/// </summary>
		public int Code { get; }

		/// <inheritdoc/>
		public override string Message { get; }

		/// <summary>
		/// Construct a new <see cref="WebSocketErroredException"/> with a generic error message and code -1.
		/// </summary>
		public WebSocketErroredException() : this("The web socket errored.", -1) { }

		/// <summary>
		/// Construct a new <see cref="WebSocketErroredException"/> with the given error message and code -1.
		/// </summary>
		public WebSocketErroredException(string message) : this(message, -1) { }

		/// <summary>
		/// Construct a new <see cref="WebSocketErroredException"/> with the given error message and code.
		/// </summary>
		public WebSocketErroredException(string message, int code) : base(string.IsNullOrWhiteSpace(message) ? (DiscordErrorMessages.ContainsKey((DiscordGatewayEventCode)code) ? DiscordErrorMessages[(DiscordGatewayEventCode)code] : string.Empty) : message) {
			Code = code;
			string? displayName = Enum.GetName(typeof(WebSocketCloseStatus), code);
			displayName ??= Enum.GetName(typeof(DiscordGatewayEventCode), code);
			Message = $"ERROR {Code} [{displayName ?? "Unnamed Error"}] :: {Message}";
		}

		/// <summary>
		/// Creates a <see cref="WebSocketErroredException"/> from the given <see cref="DiscordGatewayEventCode"/>, which includes automatic error messages.
		/// </summary>
		/// <param name="errCode"></param>
		public WebSocketErroredException(DiscordGatewayEventCode errCode) : this(DiscordErrorMessages.GetOrDefault(errCode, "The web socket errored."), (int)errCode) { }

		/// <summary>
		/// Given a <see cref="WebSocketException"/>, this will extract its code and construct a new <see cref="WebSocketErroredException"/>
		/// </summary>
		/// <param name="baseException"></param>
		/// <returns></returns>
		public static WebSocketErroredException Wrap(WebSocketException baseException) {
			return new WebSocketErroredException(baseException.Message, baseException.ErrorCode);
		}

	}
}

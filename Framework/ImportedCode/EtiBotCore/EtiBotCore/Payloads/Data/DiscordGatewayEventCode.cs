using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// A gateway code sent by Discord.
	/// </summary>
	public enum DiscordGatewayEventCode {

		/// <summary>
		/// Shit hit the fan and Discord has no idea what happened. Try again lol.
		/// </summary>
		UnknownError = 4000,

		/// <summary>
		/// YE FOCKED UP. Invalid <see cref="PayloadOpcode"/>!
		/// </summary>
		UnknownOpcode = 4001,

		/// <summary>
		/// YE FOCKED UP. Bullshittin on json encoding or message length.
		/// </summary>
		DecodeError = 4002,

		/// <summary>
		/// You sent a payload before identifying.
		/// </summary>
		NotAuthenticated = 4003,

		/// <summary>
		/// The token with your identify payload was incorrect.
		/// </summary>
		AuthenticationFailed = 4004,

		/// <summary>
		/// YE FOCKED UP. You sent two or more identify payloads.
		/// </summary>
		AlreadyAuthenticated = 4005,

		/// <summary>
		/// <strong>Voice Only.</strong> This session is no longer valid.
		/// </summary>
		SessionNoLongerValid = 4006,

		/// <summary>
		/// The sequence number given with a resume was not correct.
		/// </summary>
		InvalidResumeSequence = 4007,

		/// <summary>
		/// Bro holy shit lol slow down
		/// </summary>
		RateLimited = 4008,

		/// <summary>
		/// You know how you try talking to an old guy and he just kinda zones out? You just did that to Discord.
		/// </summary>
		TimedOut = 4009,

		/// <summary>
		/// The shard sent when identifying was malformed.
		/// </summary>
		InvalidShard = 4010,

		/// <summary>
		/// This bot would have handled far too many servers, and so you will need to shard your bot before you can identify.
		/// </summary>
		ShardingIsRequiredOrServerNotFound = 4011,

		/// <summary>
		/// The version of your API is invalid.
		/// </summary>
		InvalidAPIVersionOrUnknownProtocol = 4012,

		/// <summary>
		/// The intent was invalid.
		/// </summary>
		InvalidIntent = 4013,

		/// <summary>
		/// An intent you're implementing is one you are not authorized to use. Did you remember to enable it in the bot's dashboard? Are you verified + allowed to use it?
		/// </summary>
		NotAuthorizedToUseIntentOrVoiceDisconnected = 4014,

		/// <summary>
		/// <strong>Voice Only.</strong> The server crashed. Our bad! Try resuming.
		/// </summary>
		VoiceServerCrashed = 4015,

		/// <summary>
		/// <strong>Voice Only.</strong> Your encryption mode was not recognized.
		/// </summary>
		UnknownEncryptionMode = 4016,

	}
}

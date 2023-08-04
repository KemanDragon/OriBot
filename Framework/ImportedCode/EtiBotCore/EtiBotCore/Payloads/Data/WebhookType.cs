using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// The purpose, type, or function of a webhook.
	/// </summary>
	public enum WebhookType {

		/// <summary>
		/// This webhook is a general purpose incoming webhook that can post messages to a channel with a generated token.
		/// </summary>
		Incoming = 1,

		/// <summary>
		/// This is an internal webhook type used when following a channel, and is used to actually post the message in a different server.
		/// </summary>
		ChannelFollower = 2,

	}
}

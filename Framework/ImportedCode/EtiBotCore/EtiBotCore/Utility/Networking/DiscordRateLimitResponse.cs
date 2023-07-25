using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EtiBotCore.Utility.Networking {

	/// <summary>
	/// A json response for a rate limit.
	/// </summary>
	public class DiscordRateLimitResponse {

		/// <summary>
		/// A message saying you are being rate limited.
		/// </summary>
		[JsonProperty("message")]
		public string Message { get; private set; } = string.Empty;

		/// <summary>
		/// The amount of seconds to wait before sending another request.
		/// </summary>
		[JsonProperty("retry_after")]
		public float RetryAfter { get; private set; }

		/// <summary>
		/// Whether or not this limit is global.
		/// </summary>
		[JsonProperty("global")]
		public bool IsGlobal { get; private set; }

		/// <summary>
		/// Returns a task that will delay <em><see cref="RetryAfter"/></em> seconds, optionally adding the given padding time afterwards.
		/// </summary>
		/// <returns></returns>
		public Task Yield(int paddingMS = 500) {
			int time = (int)MathF.Floor(RetryAfter * 1000) + paddingMS;
			return Task.Delay(time);
		}

	}
}

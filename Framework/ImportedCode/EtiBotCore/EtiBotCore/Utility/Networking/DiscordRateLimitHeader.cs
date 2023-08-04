using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace EtiBotCore.Utility.Networking {

	/// <summary>
	/// Represents the header returned when Discord replies to the bot, which contains the limitations.
	/// </summary>
	public class DiscordRateLimitHeader {

		/// <summary>
		/// Whether or not the Http Response was missing the data here.
		/// </summary>
		public bool Empty { get; }

		/// <summary>
		/// Whether or not this response was accompanied with HTTP 429, meaning the bot was rate limited and needs to stop all outgoing network requests for a while.
		/// </summary>
		public bool WasRateLimited { get; }

		/// <summary>
		/// Whether or not this limit is global. This should only be parsed if <see cref="WasRateLimited"/> is <see langword="true"/>.
		/// </summary>
		public bool Global { get; }

		/// <summary>
		/// The number of requests that can be made to this endpoint in total.
		/// </summary>
		public int Limit { get; }

		/// <summary>
		/// The number of remaining requests that can be made.
		/// </summary>
		public int Remaining { get; }

		/// <summary>
		/// Epoch time at which the rate limit resets. This should only be parsed if <see cref="WasRateLimited"/> is <see langword="true"/>.
		/// </summary>
		public double Reset { get; }

		/// <summary>
		/// Total time (in seconds) of when the current rate limit bucket will reset. Can have decimals to match previous millisecond ratelimit precision.
		/// </summary>
		public double ResetAfter { get; }

		/// <summary>
		/// A unique string denoting the rate limit being encountered. This is a bucket. (Dear god).
		/// </summary>
		public string Bucket { get; }

		/// <summary>
		/// Constructs a new <see cref="DiscordRateLimitHeader"/> from the given <see cref="HttpResponseMessage"/>
		/// </summary>
		/// <param name="from"></param>
		public DiscordRateLimitHeader(HttpResponseMessage from) {
			bool foundAnything = false;
			if (from.Headers.TryGetValues("X-RateLimit-Global", out IEnumerable<string> global)) {
				Global = bool.Parse(global?.FirstOrDefault() ?? "false");
				foundAnything = true;
			}
			if (from.Headers.TryGetValues("X-RateLimit-Limit", out IEnumerable<string> limit)) {
				Limit = int.Parse(limit?.FirstOrDefault() ?? "0");
				foundAnything = true;
			}
			if (from.Headers.TryGetValues("X-RateLimit-Remaining", out IEnumerable<string> remaining)) {
				Remaining = int.Parse(remaining?.FirstOrDefault() ?? "0");
				foundAnything = true;
			}
			if (from.Headers.TryGetValues("X-RateLimit-Reset", out IEnumerable<string> reset)) {
				Reset = double.Parse(reset?.FirstOrDefault() ?? "0");
				foundAnything = true;
			}
			if (from.Headers.TryGetValues("X-RateLimit-Reset-After", out IEnumerable<string> after)) {
				ResetAfter = double.Parse(after?.FirstOrDefault() ?? "0");
				foundAnything = true;
			}
			if (from.Headers.TryGetValues("X-RateLimit-Bucket", out IEnumerable<string> bucket)) {
				Bucket = bucket?.FirstOrDefault() ?? string.Empty;
				foundAnything = true;
			} else {
				Bucket = string.Empty;
			}

			if (!foundAnything) {
				Empty = true;
				return;
			}

			WasRateLimited = from.StatusCode == HttpStatusCode.TooManyRequests;
		}
		
	}
}

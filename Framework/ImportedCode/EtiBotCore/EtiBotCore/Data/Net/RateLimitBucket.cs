using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Utility.Networking;
using EtiLogger.Logging;

namespace EtiBotCore.Data.Net {

	/// <summary>
	/// Represents a rate-limit bucket, which manages how many requests can be sent to an endpoint and when.
	/// </summary>
	public class RateLimitBucket {

		private static Dictionary<string, RateLimitBucket> BucketCache = new Dictionary<string, RateLimitBucket>();

		/// <summary>
		/// The current epoch in seconds, including millisecond decimal precision.
		/// </summary>
		public static double Epoch {
			get {
				DateTimeOffset now = DateTimeOffset.UtcNow;
				long baseTime = now.ToUnixTimeSeconds();
				long baseTimeMS = now.ToUnixTimeMilliseconds();
				long ms = baseTimeMS - baseTime;
				return baseTime + (ms / 1000D);
			}
		}

		/// <summary>
		/// Whether or not this bucket is being rate limited and cannot handle requests.
		/// </summary>
		public bool BeingRateLimited => RateLimitDuration > 0; //ResetAt >= EpochSeconds;

		/// <summary>
		/// The name of this bucket.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The number of requests that can be made to this bucket per reset interval.
		/// </summary>
		public int Capacity { get; internal set; }

		/// <summary>
		/// The number of remaining requests.
		/// </summary>
		public int Remaining { get; internal set; }

		/// <summary>
		/// How many seconds until this bucket refills. Can have decimals.
		/// </summary>
		public double RefillAfter { get; internal set; }

		/// <summary>
		/// If this is being rate limited, this is when the limit gets removed (epoch in seconds).
		/// </summary>
		public double ResetAt { get; internal set; }

		/// <summary>
		/// How many milliseconds are left in the current rate limit, or 0 if there is no limit.
		/// </summary>
		public int RateLimitDuration => (int)Math.Max(ResetAt - Epoch, 0) * 1000;

		/// <summary>
		/// When the next refill occurs.
		/// </summary>
		private double RefillOccursAt = 0;

		/// <summary>
		/// Yields for any rate limits or for the request bucket to refill if needed, and then spends one request.
		/// </summary>
		public async Task YieldAndPerform() {
			if (Capacity == 0) return;

			if (BeingRateLimited) {
				// Being rate limited? Wait until that's not happening.
				await Task.Delay(RateLimitDuration);
			}
			if (Epoch > RefillOccursAt) {
				// We haven't made a request for some time, and a refill has occurred since that last request. Refill.
				Remaining = Capacity;
			}
			if (Remaining == 0) {
				// Out of requests. Yield until it resets.
				int timeMS = (int)Math.Floor((RefillOccursAt - Epoch) * 1000);
				await Task.Delay(timeMS);

				// Then refill since we've waited for one.
				Remaining = Capacity;
			}
			Remaining--;
		}

		/// <summary>
		/// Updates all values in this bucket from the given header.
		/// </summary>
		/// <param name="rlHeader"></param>
		internal void Update(DiscordRateLimitHeader rlHeader) {
			Capacity = rlHeader.Limit;
			Remaining = rlHeader.Remaining;
			RefillAfter = rlHeader.ResetAfter;
			if (rlHeader.WasRateLimited) {
				ResetAt = rlHeader.Reset;
			}
		}

		private RateLimitBucket(DiscordRateLimitHeader rlHeader) {
			Name = rlHeader.Bucket;
			Update(rlHeader);
		}

		/// <summary>
		/// Gets an existing bucket from this header and updates it from the given header, or creates a new one.<para/>
		/// This will return <see langword="null"/> if this is a global bucket (granted a bucket doesn't exist already) or if the header input was empty. If one does already exist, it will be returned, but not updated.
		/// </summary>
		/// <param name="rlHeader"></param>
		/// <returns></returns>
		public static RateLimitBucket? GetAndUpdateOrCreate(DiscordRateLimitHeader rlHeader) {
			if (rlHeader.Empty) return null;
			if (BucketCache.TryGetValue(rlHeader.Bucket, out RateLimitBucket? bucket)) {
				if (!rlHeader.Global) bucket!.Update(rlHeader);
				return bucket!;
			}
			if (rlHeader.Global) return null;
			bucket = new RateLimitBucket(rlHeader);
			BucketCache[bucket.Name] = bucket;
			return bucket;
		}

	}
}

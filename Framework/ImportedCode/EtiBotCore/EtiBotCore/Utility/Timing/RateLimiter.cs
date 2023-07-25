using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtiBotCore.Utility.Timing {

	/// <summary>
	/// A utility designed to throttle events and yield if needed.
	/// </summary>
	public sealed class RateLimiter {

		/// <summary>
		/// The shortest amount of time between actions that can be made.
		/// </summary>
		public int DelayTimeMillis { get; set; }

		private long LastActionRequestedAtEpoch = 0;
		private long NextActionCanExecuteAtEpoch = 0;
		private int QueueSize = 0;

		/// <summary>
		/// Request that an action be performed. This returns a task that will yield depending on the number of pending actions.
		/// </summary>
		public async Task RequestPerformAction() {
			QueueSize++;

			long nowMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			long timeDiff = nowMillis - LastActionRequestedAtEpoch;
			LastActionRequestedAtEpoch = nowMillis;

			if (timeDiff < DelayTimeMillis) {
				NextActionCanExecuteAtEpoch = nowMillis + (DelayTimeMillis - timeDiff);
				if (QueueSize > 1) {
					NextActionCanExecuteAtEpoch += DelayTimeMillis * (QueueSize - 1);
				}
			} else {
				NextActionCanExecuteAtEpoch = LastActionRequestedAtEpoch;
			}

			/*
			if (timeDiff < DelayTimeMillis) {
				int delayTime = (int)(DelayTimeMillis - timeDiff);
				if (delayTime > 0) {
					await Task.Delay(delayTime);
				}
			}
			*/
			int delayTime = (int)(NextActionCanExecuteAtEpoch - LastActionRequestedAtEpoch);
			await Task.Delay(delayTime);
			if (QueueSize > 1) {
				// Do >1 because calling Task.Delay(0) is worse than useless.
				await Task.Delay(DelayTimeMillis * (QueueSize - 1));
			}
			QueueSize--;
		}

	}
}

using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Exceptions.Marshalling;

namespace EtiBotCore.Clockwork {

	/// <summary>
	/// A system that can be used to limit the rate of an arbitrary operation.
	/// </summary>
	public class OperationClockwork {

		/// <summary>
		/// The time that must be delayed between attempts at setting this in milliseconds.
		/// </summary>
		public int Interval { get; }

		/// <summary>
		/// The last epoch that this was set at, in milliseconds.
		/// </summary>
		public long LastEpochSet { get; private set; } = 0;

		/// <summary>
		/// Signals that this operation has ticked, and updates the timer internally that limits the rate.<para/>
		/// Throws an <see cref="EditingTooFastException"/> if this is called more than once per <see cref="Interval"/> milliseconds.
		/// </summary>
		/// <exception cref="EditingTooFastException">If this called more than once per <see cref="Interval"/> milliseconds</exception>
		public void OperationPerformed() {
			long epochNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			if ((epochNow - LastEpochSet) < Interval) {
				throw new EditingTooFastException(Interval, epochNow - LastEpochSet);
			}
			LastEpochSet = epochNow;
		}

		/// <summary>
		/// Construct a new <see cref="OperationClockwork"/> with the given interval.<para/>
		/// The interval's default is one minute.
		/// </summary>
		/// <param name="interval">The interval between when this property can be set in milliseconds.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="interval"/> is less than zero.</exception>
		public OperationClockwork(int interval = 60000) {
			if (interval < 0) throw new ArgumentOutOfRangeException(nameof(interval));
			Interval = interval;
		}

	}
}

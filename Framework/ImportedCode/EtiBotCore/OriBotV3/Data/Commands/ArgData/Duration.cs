using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.Utility;

namespace OldOriBot.Data.Commands.ArgData {

	/// <summary>
	/// A duration of time.
	/// </summary>
	[Serializable]
	public class Duration : ICommandArg<Duration> {

		/// <summary>
		/// <see langword="true"/> if this <see cref="Duration"/> was not properly constructed due to a malformed time string.
		/// </summary>
		public bool Malformed { get; } = true;

		/// <summary>
		/// The length of this duration in seconds.
		/// </summary>
		public ulong TimeInSeconds { get; }

		/// <summary>
		/// The length of this duration in the given unit of time (see <see cref="Unit"/>)
		/// </summary>
		public ulong TimeInGivenUnit { get; }

		/// <summary>
		/// The given unit.
		/// </summary>
		public TimeUnit Unit { get; }

		/// <summary>
		/// The duration represented as a TimeSpan
		/// </summary>
		public TimeSpan TimeSpan { get; }

		/// <summary>
		/// Returns a UTC <see cref="DateTimeOffset"/> in the future computed from <c><see cref="DateTimeOffset.UtcNow"/> + <see cref="TimeSpan"/></c>
		/// </summary>
		public DateTimeOffset InFuture => DateTimeOffset.UtcNow + TimeSpan;

		public Duration() { }

		private Duration(string timeStr) {
			Malformed = !TimeExtensions.GetTimeFromText(timeStr, out ulong timeSecs, out TimeUnit unit, out ulong baseUnit, false);
			TimeInSeconds = timeSecs;
			TimeInGivenUnit = baseUnit;
			Unit = unit;
			TimeSpan = TimeSpan.FromSeconds(timeSecs);
		}
		
		Duration ICommandArg<Duration>.From(string instance, object inContext) {
			return new Duration(instance);
		}

		public object From(string instance, object inContext) => ((ICommandArg<Duration>)this).From(instance, inContext);
	}
}

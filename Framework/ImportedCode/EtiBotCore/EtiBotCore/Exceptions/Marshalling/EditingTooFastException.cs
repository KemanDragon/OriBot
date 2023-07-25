using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Exceptions.Marshalling {

	/// <summary>
	/// An exception that is thrown if a property is edited too fast.<para/>
	/// This is applied to properties that send network requests that are sensitive to things like caching.
	/// </summary>
	internal class EditingTooFastException : Exception {

		/// <summary>
		/// The time in milliseconds that must be delayed before this property can be edited again.
		/// </summary>
		public int Interval { get; }

		/// <summary>
		/// The time in millisconds that you must delay before attempting to retry this operation.
		/// </summary>
		public int DelayTime { get; }

		/// <summary>
		/// Construct a new <see cref="EditingTooFastException"/> that relays that this property can only be edited once every <paramref name="intervalMilliseconds"/> milliseconds.
		/// </summary>
		/// <param name="intervalMilliseconds">The minimum amount of time that can be delayed between setting the associated property throwing this exception.</param>
		/// <param name="delayTime">The remaining time on the operation that needs to be delayed for until performing it gain.</param>
		public EditingTooFastException(int intervalMilliseconds, long delayTime) : base($"You are editing this property too fast! Please wait at least {intervalMilliseconds}ms between calls to set this property.") {
			Interval = intervalMilliseconds;
			DelayTime = (int)delayTime;
		}

	}
}

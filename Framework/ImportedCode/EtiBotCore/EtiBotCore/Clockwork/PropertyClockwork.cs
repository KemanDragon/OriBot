using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Exceptions.Marshalling;

#nullable disable
namespace EtiBotCore.Clockwork {
	/// <summary>
	/// A class that manages the updates performed on a property, limiting the interval in which it can be set.<para/>
	/// Consider using the typed variant <see cref="PropertyClockwork{T}"/> if possible.
	/// </summary>
	[Obsolete] public class PropertyClockwork {

		/// <summary>
		/// The time that must be delayed between attempts at setting this in milliseconds.
		/// </summary>
		public int Interval { get; }

		/// <summary>
		/// The last epoch that this was set at, in milliseconds.
		/// </summary>
		public long LastEpochSet { get; set; } = 0;
		
		/// <summary>
		/// The value stored within this property.<para/>
		/// <see langword="set"/> may throw an <see cref="EditingTooFastException"/> if this property is edited more than once per <see cref="Interval"/> milliseconds.
		/// </summary>
		/// <exception cref="EditingTooFastException">If this property is set more than once per <see cref="Interval"/> milliseconds</exception>
		public object Value {
			get => _Value;
			set {
				long epochNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();
				if ((epochNow - LastEpochSet) <= Interval) {
					throw new EditingTooFastException(Interval, (epochNow - LastEpochSet));
				}
				LastEpochSet = epochNow;
				_Value = value;
			}
		}
		private object _Value = null;

		/// <summary>
		/// An alias method to <see cref="Value"/>.<see langword="get"/>.
		/// </summary>
		/// <returns></returns>
		public object Get() => Value;

		/// <summary>
		/// An alias method to <see cref="Value"/>.<see langword="set"/>.
		/// </summary>
		/// <param name="value"></param>
		public void Set(object value) => Value = value;


		/// <summary>
		/// Construct a new <see cref="PropertyClockwork"/> with the given interval and default value.<para/>
		/// The interval's default is one minute.
		/// </summary>
		/// <param name="interval">The interval between when this property can be set in milliseconds.</param>
		/// <param name="defaultValue">The default value that this property is initialized with.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="interval"/> is less than zero.</exception>
		public PropertyClockwork(int interval = 60000, object defaultValue = default) {
			if (interval < 0) throw new ArgumentOutOfRangeException(nameof(interval));
			Interval = interval;
			_Value = defaultValue;
		}

		/// <summary>
		/// Creates a nwe <see cref="PropertyClockwork"/> with the that lasts one minute with the given default value.
		/// </summary>
		/// <param name="defaultValue">The default value that this property is initialized with.</param>
		public PropertyClockwork(object defaultValue) : this(60000, defaultValue) { }

	}
	
	/// <summary>
	/// A class that manages the updates performed on a property, limiting the interval in which it can be set.
	/// </summary>
	/// <typeparam name="T">The type of object that this clockwork looks over.</typeparam>
	[Obsolete] public class PropertyClockwork<T> : PropertyClockwork {

		/// <summary>
		/// The value stored within this property.<para/>
		/// <see langword="set"/> may throw an <see cref="EditingTooFastException"/> if this property is edited more than once per  milliseconds.
		/// </summary>
		/// <exception cref="EditingTooFastException">If this property is set more than once per  milliseconds</exception>
		public new T Value {
			get => _Value;
			set {
				long epochNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();
				if ((epochNow - LastEpochSet) <= Interval) {
					throw new EditingTooFastException(Interval, (epochNow - LastEpochSet));
				}
				LastEpochSet = epochNow;
				_Value = value;
			}
		}
		private T _Value = default; // Ignore null stuffs.

		/// <summary>
		/// An alias method to <see cref="Value"/>.<see langword="get"/>.
		/// </summary>
		/// <returns></returns>
		public new T Get() => Value;

		/// <summary>
		/// An alias method to <see cref="Value"/>.<see langword="set"/>.
		/// </summary>
		/// <param name="value"></param>
		public void Set(T value) => Value = value;

#pragma warning disable IDE0051 // Remove unused private members
		/// <summary>
		/// Not implemented in <see cref="PropertyClockwork{T}"/> -- This is defined to hide it from the inherited <see cref="PropertyClockwork"/>.
		/// </summary>
		/// <param name="_"></param>
		private new void Set(object _) { }
#pragma warning restore IDE0051 // Remove unused private members

		/// <summary>
		/// Construct a new <see cref="PropertyClockwork{T}"/> with the given interval and default value.<para/>
		/// The interval's default is one minute.
		/// </summary>
		/// <param name="interval">The interval between when this property can be set in milliseconds.</param>
		/// <param name="defaultValue">The default value that this property is initialized with.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="interval"/> is less than zero.</exception>
		public PropertyClockwork(int interval = 60000, T defaultValue = default) : base(interval) {
			_Value = defaultValue;
		}

		/// <summary>
		/// Construct a new <see cref="PropertyClockwork{T}"/> that lasts one minute with the given default value.
		/// </summary>
		/// <param name="defaultValue">The default value that this property is initialized with.</param>
		public PropertyClockwork(T defaultValue) : this(60000, defaultValue) { }

	}
}

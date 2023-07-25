using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Exceptions.Marshalling;

#nullable disable
namespace EtiBotCore.Utility.Marshalling {

	/// <summary>
	/// A utility that allows locking/unlocking a property's setter.
	/// </summary>
	public class ManagedSetter<T> {

		/// <summary>
		/// The value stored within this <see cref="ManagedSetter{T}"/>
		/// </summary>
		/// <exception cref="PropertyLockedException">If this is locked.</exception>
		public T Value {
			get => _Value;
			set {
				if (Locked) throw new PropertyLockedException();
				_Value = value;
			}
		}
		private T _Value = default;

		/// <summary>
		/// Whether or not the setter is locked.<para/>
		/// Attempting to call <see cref="Locked"/>.<see langword="set"/> while <see cref="LockedDelegate"/> is not <see langword="null"/> will raise an <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the setter is called and <see cref="LockedDelegate"/> is not <see langword="null"/>.</exception>
		public bool Locked {
			get {
				if (LockedDelegate == null) return _Locked;
				return LockedDelegate();
			}
			set {
				if (LockedDelegate != null) throw new InvalidOperationException("The locked delegate has been set to a non-null value, so changing this value is not allowed.");
				_Locked = value;
			}
		}
		private bool _Locked = false;

		/// <summary>
		/// A delegate that is used to tell this object whether or not it's locked. Set to <see langword="null"/> to use the property <see cref="Locked"/> instead.
		/// </summary>
		public Func<bool> LockedDelegate { get; set; } = null;


		/// <summary>
		/// Construct a new <see cref="ManagedSetter{T}"/> with the given settings.
		/// </summary>
		/// <param name="defaultValue">The value that the field will store.</param>
		/// <param name="startLocked">Whether or not this starts out as locked.</param>
		public ManagedSetter(T defaultValue = default, bool startLocked = true) {
			_Value = defaultValue;
			_Locked = startLocked;
		}

		/// <summary>
		/// Construct a new <see cref="ManagedSetter{T}"/> with the given settings.
		/// </summary>
		/// <param name="defaultValue">The value that the field will store.</param>
		/// <param name="lockedDelegate">The delegate to use to determine whether or not this is locked.</param>
		public ManagedSetter(Func<bool> lockedDelegate, T defaultValue = default) {
			_Value = defaultValue;
			LockedDelegate = lockedDelegate;
		}

	}
}

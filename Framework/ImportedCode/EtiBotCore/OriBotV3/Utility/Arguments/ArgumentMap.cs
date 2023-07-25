using System;
using System.Collections.Generic;
using System.Text;

namespace OldOriBot.Utility.Arguments {

	/// <summary>
	/// Represents arguments passed into a command.
	/// </summary>
	/// <typeparam name="T1">The type of the first argument.</typeparam>
	/// <typeparam name="T2">The type of the second argument.</typeparam>
	/// <typeparam name="T3">The type of the third argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth argument.</typeparam>
	/// <typeparam name="T5">The type of the fifth argument.</typeparam>
	public abstract class ArgumentMap {

		/// <summary>
		/// Construct a new <see cref="ArgumentMap"/> from the given values.
		/// </summary>
		/// <param name="arg1">The first argument.</param>
		/// <param name="arg2">The second argument.</param>
		/// <param name="arg3">The third argument.</param>
		/// <param name="arg4">The fourth argument.</param>
		/// <param name="arg5">The fifth argument.</param>
		protected ArgumentMap() { }
		
	}

	/// <inheritdoc cref="ArgumentMap"/>
	public class ArgumentMap<T1> : ArgumentMap {

		/// <summary>
		/// The first argument in this map.
		/// </summary>
		public T1 Arg1 { get; }

		/// <inheritdoc cref="ArgumentMap.ArgumentMap"/>
		public ArgumentMap(T1 arg1) {
			Arg1 = arg1;
		}

	}

	/// <inheritdoc cref="ArgumentMap"/>
	public class ArgumentMap<T1, T2> : ArgumentMap<T1> {

		/// <summary>
		/// The second argument in this map.
		/// </summary>
		public T2 Arg2 { get; }

		/// <inheritdoc cref="ArgumentMap.ArgumentMap"/>
		public ArgumentMap(T1 arg1, T2 arg2) : base(arg1) {
			Arg2 = arg2;
		}

	}

	/// <inheritdoc cref="ArgumentMap"/>
	public class ArgumentMap<T1, T2, T3> : ArgumentMap<T1, T2> {

		/// <summary>
		/// The third argument in this map.
		/// </summary>
		public T3 Arg3 { get; }

		/// <inheritdoc cref="ArgumentMap.ArgumentMap"/>
		public ArgumentMap(T1 arg1, T2 arg2, T3 arg3) : base(arg1, arg2) {
			Arg3 = arg3;
		}

	}

	/// <inheritdoc cref="ArgumentMap"/>
	public class ArgumentMap<T1, T2, T3, T4> : ArgumentMap<T1, T2, T3> {

		/// <summary>
		/// The fourth argument in this map.
		/// </summary>
		public T4 Arg4 { get; }

		/// <inheritdoc cref="ArgumentMap.ArgumentMap"/>
		public ArgumentMap(T1 arg1, T2 arg2, T3 arg3, T4 arg4) : base(arg1, arg2, arg3) {
			Arg4 = arg4;
		}

	}

	/// <inheritdoc cref="ArgumentMap"/>
	public class ArgumentMap<T1, T2, T3, T4, T5> : ArgumentMap<T1, T2, T3, T4> {

		/// <summary>
		/// The fifth argument in this map.
		/// </summary>
		public T5 Arg5 { get; }

		/// <inheritdoc cref="ArgumentMap.ArgumentMap"/>
		public ArgumentMap(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) : base(arg1, arg2, arg3, arg4) {
			Arg5 = arg5;
		}

	}

}

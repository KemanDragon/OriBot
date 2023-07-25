using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SignalCore {

	/// <summary>
	/// A utility class that yields for a <see cref="Signal"/>'s wait call.
	/// </summary>
	public class Waiter {

		protected readonly ManualResetEventSlim Event = new ManualResetEventSlim();

		// No result

		/// <summary>
		/// Yields the current thread until this <see cref="Waiter"/> has been released.
		/// </summary>
		public void Yield() {
			Event.Wait();
		}

		/// <summary>
		/// Releases this <see cref="Waiter"/> and allows execution to continue.
		/// </summary>
		public void Release() {
			Event.Set();
		}

	}

	/// <inheritdoc cref="Waiter"/>
	public class Waiter<T1> : Waiter {

		public T1 Result { get; private set; }

		/// <summary>
		/// Yields the current thread until this <see cref="Waiter"/> has been released.
		/// </summary>
		public new T1 Yield() {
			Event.Wait();
			return Result;
		}

		/// <summary>
		/// Releases this <see cref="Waiter"/> and allows execution to continue.
		/// </summary>
		public void Release(T1 t1) {
			Event.Set();
			Result = t1;
		}

	}

	/// <inheritdoc cref="Waiter"/>
	public class Waiter<T1, T2> : Waiter {

		public (T1, T2) Result { get; private set; }

		/// <summary>
		/// Yields the current thread until this <see cref="Waiter"/> has been released.
		/// </summary>
		public new(T1, T2) Yield() {
			Event.Wait();
			return Result;
		}

		/// <summary>
		/// Releases this <see cref="Waiter"/> and allows execution to continue.
		/// </summary>
		public void Release(T1 t1, T2 t2) {
			Event.Set();
			Result = (t1, t2);
		}

	}

	/// <inheritdoc cref="Waiter"/>
	public class Waiter<T1, T2, T3> : Waiter {

		public (T1, T2, T3) Result { get; private set; }

		/// <summary>
		/// Yields the current thread until this <see cref="Waiter"/> has been released.
		/// </summary>
		public new(T1, T2, T3) Yield() {
			Event.Wait();
			return Result;
		}

		/// <summary>
		/// Releases this <see cref="Waiter"/> and allows execution to continue.
		/// </summary>
		public void Release(T1 t1, T2 t2, T3 t3) {
			Event.Set();
			Result = (t1, t2, t3);
		}
	}

	/// <inheritdoc cref="Waiter"/>
	public class Waiter<T1, T2, T3, T4> : Waiter {

		public (T1, T2, T3, T4) Result { get; private set; }

		/// <summary>
		/// Yields the current thread until this <see cref="Waiter"/> has been released.
		/// </summary>
		public new(T1, T2, T3, T4) Yield() {
			Event.Wait();
			return Result;
		}

		/// <summary>
		/// Releases this <see cref="Waiter"/> and allows execution to continue.
		/// </summary>
		public void Release(T1 t1, T2 t2, T3 t3, T4 t4) {
			Event.Set();
			Result = (t1, t2, t3, t4);
		}
	}

	/// <inheritdoc cref="Waiter"/>
	public class Waiter<T1, T2, T3, T4, T5> : Waiter {

		public (T1, T2, T3, T4, T5) Result { get; private set; }

		/// <summary>
		/// Yields the current thread until this <see cref="Waiter"/> has been released.
		/// </summary>
		public new(T1, T2, T3, T4, T5) Yield() {
			Event.Wait();
			return Result;
		}

		/// <summary>
		/// Releases this <see cref="Waiter"/> and allows execution to continue.
		/// </summary>
		public void Release(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) {
			Event.Set();
			Result = (t1, t2, t3, t4, t5);
		}
	}


}

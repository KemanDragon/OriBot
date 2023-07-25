using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SignalCore {

	/// <summary>
	/// Represents a connection to a <see cref="Signal"/>
	/// </summary>
	public sealed class Connection : IDisposable {

		private Signal Source;

		internal Func<Task> Method;

		internal Connection(Signal source, Func<Task> del) {
			Source = source;
			Method = del;
		}

		internal Task Fire() {
			return Method?.Invoke();
		}

		/// <summary>
		/// Disconnects this <see cref="Connection"/>, causing its callback to no longer execute when its parent <see cref="Signal"/> is fired.
		/// </summary>
		/// <remarks>
		/// This renders the connection unusable, as it calls <see cref="Dispose"/>
		/// </remarks>
		public void Disconnect() => Dispose();

		/// <summary>
		/// Disconnects this <see cref="Connection"/>, causing its callback to no longer execute when its parent <see cref="Signal"/> is fired.
		/// </summary>
		public void Dispose() {
			Source.Disconnect(this);
			Source = null;
			Method = null;
		}
	}

	/// <summary>
	/// <inheritdoc cref="Connection"/>
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	public sealed class Connection<T1> : IDisposable {

		private Signal<T1> Source;

		internal Func<T1, Task> Method;

		internal Connection(Signal<T1> source, Func<T1, Task> del) {
			Source = source;
			Method = del;
		}

		internal Task Fire(T1 param1) {
			return Method?.Invoke(param1);
		}

		/// <summary>
		/// Disconnects this <see cref="Connection"/>, causing its callback to no longer execute when its parent <see cref="Signal"/> is fired.
		/// </summary>
		/// <remarks>
		/// This renders the connection unusable, as it calls <see cref="Dispose"/>
		/// </remarks>
		public void Disconnect() => Dispose();

		/// <summary>
		/// Disconnects this <see cref="Connection"/>, causing its callback to no longer execute when its parent <see cref="Signal"/> is fired.
		/// </summary>
		public void Dispose() {
			Source.Disconnect(this);
			Source = null;
			Method = null;
		}
	}

	/// <summary>
	/// <inheritdoc cref="Connection"/>
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	public sealed class Connection<T1, T2> : IDisposable {

		private Signal<T1, T2> Source;

		internal Func<T1, T2, Task> Method;

		internal Connection(Signal<T1, T2> source, Func<T1, T2, Task> del) {
			Source = source;
			Method = del;
		}

		internal Task Fire(T1 param1, T2 param2) {
			return Method?.Invoke(param1, param2);
		}

		/// <summary>
		/// Disconnects this <see cref="Connection"/>, causing its callback to no longer execute when its parent <see cref="Signal"/> is fired.
		/// </summary>
		/// <remarks>
		/// This renders the connection unusable, as it calls <see cref="Dispose"/>
		/// </remarks>
		public void Disconnect() => Dispose();

		/// <summary>
		/// Disconnects this <see cref="Connection"/>, causing its callback to no longer execute when its parent <see cref="Signal"/> is fired.
		/// </summary>
		public void Dispose() {
			Source.Disconnect(this);
			Source = null;
			Method = null;
		}
	}

	/// <summary>
	/// <inheritdoc cref="Connection"/>
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	public sealed class Connection<T1, T2, T3> : IDisposable {

		private Signal<T1, T2, T3> Source;

		internal Func<T1, T2, T3, Task> Method;

		internal Connection(Signal<T1, T2, T3> source, Func<T1, T2, T3, Task> del) {
			Source = source;
			Method = del;
		}

		internal Task Fire(T1 param1, T2 param2, T3 param3) {
			return Method?.Invoke(param1, param2, param3);
		}

		/// <summary>
		/// Disconnects this <see cref="Connection"/>, causing its callback to no longer execute when its parent <see cref="Signal"/> is fired.
		/// </summary>
		/// <remarks>
		/// This renders the connection unusable, as it calls <see cref="Dispose"/>
		/// </remarks>
		public void Disconnect() => Dispose();

		/// <summary>
		/// Disconnects this <see cref="Connection"/>, causing its callback to no longer execute when its parent <see cref="Signal"/> is fired.
		/// </summary>
		public void Dispose() {
			Source.Disconnect(this);
			Source = null;
			Method = null;
		}
	}

	/// <summary>
	/// <inheritdoc cref="Connection"/>
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	public sealed class Connection<T1, T2, T3, T4> : IDisposable {

		private Signal<T1, T2, T3, T4> Source;

		internal Func<T1, T2, T3, T4, Task> Method;

		internal Connection(Signal<T1, T2, T3, T4> source, Func<T1, T2, T3, T4, Task> del) {
			Source = source;
			Method = del;
		}

		internal Task Fire(T1 param1, T2 param2, T3 param3, T4 param4) {
			return Method?.Invoke(param1, param2, param3, param4);
		}

		/// <summary>
		/// Disconnects this <see cref="Connection"/>, causing its callback to no longer execute when its parent <see cref="Signal"/> is fired.
		/// </summary>
		/// <remarks>
		/// This renders the connection unusable, as it calls <see cref="Dispose"/>
		/// </remarks>
		public void Disconnect() => Dispose();

		/// <summary>
		/// Disconnects this <see cref="Connection"/>, causing its callback to no longer execute when its parent <see cref="Signal"/> is fired.
		/// </summary>
		public void Dispose() {
			Source.Disconnect(this);
			Source = null;
			Method = null;
		}
	}

	/// <summary>
	/// <inheritdoc cref="Connection"/>
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	public sealed class Connection<T1, T2, T3, T4, T5> : IDisposable {

		private Signal<T1, T2, T3, T4, T5> Source;

		internal Func<T1, T2, T3, T4, T5, Task> Method;

		internal Connection(Signal<T1, T2, T3, T4, T5> source, Func<T1, T2, T3, T4, T5, Task> del) {
			Source = source;
			Method = del;
		}

		internal Task Fire(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5) {
			return Method?.Invoke(param1, param2, param3, param4, param5);
		}

		/// <summary>
		/// Disconnects this <see cref="Connection"/>, causing its callback to no longer execute when its parent <see cref="Signal"/> is fired.
		/// </summary>
		/// <remarks>
		/// This renders the connection unusable, as it calls <see cref="Dispose"/>
		/// </remarks>
		public void Disconnect() => Dispose();

		/// <summary>
		/// Disconnects this <see cref="Connection"/>, causing its callback to no longer execute when its parent <see cref="Signal"/> is fired.
		/// </summary>
		public void Dispose() {
			Source.Disconnect(this);
			Source = null;
			Method = null;
		}
	}
}

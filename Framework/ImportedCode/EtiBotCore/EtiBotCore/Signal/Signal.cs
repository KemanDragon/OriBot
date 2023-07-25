using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SignalCore {

	/// <summary>
	/// Provides a means of creating asynchronous events in a manner akin to <a href="https://developer.roblox.com/en-us/api-reference/datatype/RBXScriptSignal">RBXScriptSignal</a>.
	/// </summary>
	public sealed class Signal : IDisposable {

		private readonly SynchronizedCollection<Connection> Connections = new SynchronizedCollection<Connection>();
		private readonly Waiter Waiter = new Waiter();

		/// <summary>
		/// Connects the given action to this <see cref="Signal"/>, returning a <see cref="Connection"/> instance that can be used to disconnect from this signal.
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public Connection Connect(Func<Task> action) {
			Connection con = new Connection(this, action);
			Connections.Add(con);
			return con;
		}

		/// <summary>
		/// Returns a task that yields until this <see cref="Signal"/> is invoked.
		/// </summary>
		/// <returns></returns>
		public Task Wait() {
			return Task.Run(() => {
				Waiter.Yield();
			});
		}

		/// <summary>
		/// Asynchronously executes all <see cref="Connections"/>.
		/// </summary>
		/// <param name="objs"></param>
		/// <returns></returns>
		public async Task Invoke() {
			Waiter.Release();
			foreach (Connection con in Connections) {
				await con.Fire();
			}
		}

		internal void Disconnect(Connection con) {
			Connections.Remove(con);
		}

		internal void Disconnect(Func<Task> method) {
			Connections.FirstOrDefault(con => con.Method == method)?.Disconnect();
		}

		/// <summary>
		/// Disconnects all event <see cref="Connection"/>s from this <see cref="Signal"/>.
		/// </summary>
		public void Dispose() {
			Connections.Clear();
		}

		public static Signal operator +(Signal left, Func<Task> right) {
			left.Connect(right);
			return left;
		}

		public static Signal operator -(Signal left, Func<Task> right) {
			left.Disconnect(right);
			return left;
		}
	}

	/// <summary>
	/// <inheritdoc cref="Signal"/>
	/// </summary>
	public sealed class Signal<T1> : IDisposable {

		private readonly List<Connection<T1>> Connections = new List<Connection<T1>>();
		private readonly Waiter<T1> Waiter = new Waiter<T1>();

		/// <summary>
		/// Connects the given action to this <see cref="Signal"/>, returning a <see cref="Connection"/> instance that can be used to disconnect from this signal.
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public Connection<T1> Connect(Func<T1, Task> action) {
			var con = new Connection<T1>(this, action);
			Connections.Add(con);
			return con;
		}

		/// <summary>
		/// Returns a task that yields until this <see cref="Signal"/> is invoked.
		/// </summary>
		/// <returns></returns>
		public Task<T1> Wait() {
			return Task.Run(() => {
				Waiter.Yield();
				return Waiter.Result;
			});
		}

		/// <summary>
		/// Asynchronously executes all <see cref="Connections"/>.
		/// </summary>
		/// <param name="objs"></param>
		/// <returns></returns>
		public async Task Invoke(T1 param1) {
			Waiter.Release(param1);
			foreach (var con in Connections) {
				await con.Fire(param1);
			}
		}

		internal void Disconnect(Connection<T1> con) {
			Connections.Remove(con);
		}

		internal void Disconnect(Func<T1, Task> method) {
			Connections.FirstOrDefault(con => con.Method == method)?.Disconnect();
		}

		/// <summary>
		/// Disconnects all event <see cref="Connection"/>s from this <see cref="Signal"/>.
		/// </summary>
		public void Dispose() {
			Connections.Clear();
		}

		public static Signal<T1> operator +(Signal<T1> left, Func<T1, Task> right) {
			left.Connect(right);
			return left;
		}

		public static Signal<T1> operator -(Signal<T1> left, Func<T1, Task> right) {
			left.Disconnect(right);
			return left;
		}
	}

	/// <summary>
	/// <inheritdoc cref="Signal"/>
	/// </summary>
	public sealed class Signal<T1, T2> : IDisposable {

		private readonly List<Connection<T1, T2>> Connections = new List<Connection<T1, T2>>();
		private readonly Waiter<T1, T2> Waiter = new Waiter<T1, T2>();

		/// <summary>
		/// Connects the given action to this <see cref="Signal"/>, returning a <see cref="Connection"/> instance that can be used to disconnect from this signal.
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public Connection<T1, T2> Connect(Func<T1, T2, Task> action) {
			var con = new Connection<T1, T2>(this, action);
			Connections.Add(con);
			return con;
		}

		/// <summary>
		/// Returns a task that yields until this <see cref="Signal"/> is invoked.
		/// </summary>
		/// <returns></returns>
		public Task<(T1, T2)> Wait() {
			return Task.Run(() => {
				Waiter.Yield();
				return Waiter.Result;
			});
		}

		/// <summary>
		/// Asynchronously executes all <see cref="Connections"/>.
		/// </summary>
		/// <param name="objs"></param>
		/// <returns></returns>
		public async Task Invoke(T1 param1, T2 param2) {
			Waiter.Release(param1, param2);
			foreach (var con in Connections) {
				await con.Fire(param1, param2);
			}
		}

		internal void Disconnect(Connection<T1, T2> con) {
			Connections.Remove(con);
		}

		internal void Disconnect(Func<T1, T2, Task> method) {
			Connections.FirstOrDefault(con => con.Method == method)?.Disconnect();
		}

		/// <summary>
		/// Disconnects all event <see cref="Connection"/>s from this <see cref="Signal"/>.
		/// </summary>
		public void Dispose() {
			Connections.Clear();
		}

		public static Signal<T1, T2> operator +(Signal<T1, T2> left, Func<T1, T2, Task> right) {
			left.Connect(right);
			return left;
		}

		public static Signal<T1, T2> operator -(Signal<T1, T2> left, Func<T1, T2, Task> right) {
			left.Disconnect(right);
			return left;
		}
	}

	/// <summary>
	/// <inheritdoc cref="Signal"/>
	/// </summary>
	public sealed class Signal<T1, T2, T3> : IDisposable {

		private readonly List<Connection<T1, T2, T3>> Connections = new List<Connection<T1, T2, T3>>();
		private readonly Waiter<T1, T2, T3> Waiter = new Waiter<T1, T2, T3>();

		/// <summary>
		/// Connects the given action to this <see cref="Signal"/>, returning a <see cref="Connection"/> instance that can be used to disconnect from this signal.
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public Connection<T1, T2, T3> Connect(Func<T1, T2, T3, Task> action) {
			var con = new Connection<T1, T2, T3>(this, action);
			Connections.Add(con);
			return con;
		}

		/// <summary>
		/// Returns a task that yields until this <see cref="Signal"/> is invoked.
		/// </summary>
		/// <returns></returns>
		public Task<(T1, T2, T3)> Wait() {
			return Task.Run(() => {
				Waiter.Yield();
				return Waiter.Result;
			});
		}

		/// <summary>
		/// Asynchronously executes all <see cref="Connections"/>.
		/// </summary>
		/// <param name="objs"></param>
		/// <returns></returns>
		public async Task Invoke(T1 param1, T2 param2, T3 param3) {
			Waiter.Release(param1, param2, param3);
			foreach (var con in Connections) {
				await con.Fire(param1, param2, param3);
			}
		}

		internal void Disconnect(Connection<T1, T2, T3> con) {
			Connections.Remove(con);
		}

		internal void Disconnect(Func<T1, T2, T3, Task> method) {
			Connections.FirstOrDefault(con => con.Method == method)?.Disconnect();
		}

		/// <summary>
		/// Disconnects all event <see cref="Connection"/>s from this <see cref="Signal"/>.
		/// </summary>
		public void Dispose() {
			Connections.Clear();
		}

		public static Signal<T1, T2, T3> operator +(Signal<T1, T2, T3> left, Func<T1, T2, T3, Task> right) {
			left.Connect(right);
			return left;
		}

		public static Signal<T1, T2, T3> operator -(Signal<T1, T2, T3> left, Func<T1, T2, T3, Task> right) {
			left.Disconnect(right);
			return left;
		}
	}

	/// <summary>
	/// <inheritdoc cref="Signal"/>
	/// </summary>
	public sealed class Signal<T1, T2, T3, T4> : IDisposable {

		private readonly List<Connection<T1, T2, T3, T4>> Connections = new List<Connection<T1, T2, T3, T4>>();
		private readonly Waiter<T1, T2, T3, T4> Waiter = new Waiter<T1, T2, T3, T4>();

		/// <summary>
		/// Connects the given action to this <see cref="Signal"/>, returning a <see cref="Connection"/> instance that can be used to disconnect from this signal.
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public Connection<T1, T2, T3, T4> Connect(Func<T1, T2, T3, T4, Task> action) {
			var con = new Connection<T1, T2, T3, T4>(this, action);
			Connections.Add(con);
			return con;
		}

		/// <summary>
		/// Returns a task that yields until this <see cref="Signal"/> is invoked.
		/// </summary>
		/// <returns></returns>
		public Task<(T1, T2, T3, T4)> Wait() {
			return Task.Run(() => {
				Waiter.Yield();
				return Waiter.Result;
			});
		}

		/// <summary>
		/// Asynchronously executes all <see cref="Connections"/>.
		/// </summary>
		/// <param name="objs"></param>
		/// <returns></returns>
		public async Task Invoke(T1 param1, T2 param2, T3 param3, T4 param4) {
			Waiter.Release(param1, param2, param3, param4);
			foreach (var con in Connections) {
				await con.Fire(param1, param2, param3, param4);
			}
		}

		internal void Disconnect(Connection<T1, T2, T3, T4> con) {
			Connections.Remove(con);
		}

		internal void Disconnect(Func<T1, T2, T3, T4, Task> method) {
			Connections.FirstOrDefault(con => con.Method == method)?.Disconnect();
		}

		/// <summary>
		/// Disconnects all event <see cref="Connection"/>s from this <see cref="Signal"/>.
		/// </summary>
		public void Dispose() {
			Connections.Clear();
		}

		public static Signal<T1, T2, T3, T4> operator +(Signal<T1, T2, T3, T4> left, Func<T1, T2, T3, T4, Task> right) {
			left.Connect(right);
			return left;
		}

		public static Signal<T1, T2, T3, T4> operator -(Signal<T1, T2, T3, T4> left, Func<T1, T2, T3, T4, Task> right) {
			left.Disconnect(right);
			return left;
		}
	}

	/// <summary>
	/// <inheritdoc cref="Signal"/>
	/// </summary>
	public sealed class Signal<T1, T2, T3, T4, T5> : IDisposable {

		private readonly List<Connection<T1, T2, T3, T4, T5>> Connections = new List<Connection<T1, T2, T3, T4, T5>>();
		private readonly Waiter<T1, T2, T3, T4, T5> Waiter = new Waiter<T1, T2, T3, T4, T5>();

		/// <summary>
		/// Connects the given action to this <see cref="Signal"/>, returning a <see cref="Connection"/> instance that can be used to disconnect from this signal.
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public Connection<T1, T2, T3, T4, T5> Connect(Func<T1, T2, T3, T4, T5, Task> action) {
			var con = new Connection<T1, T2, T3, T4, T5>(this, action);
			Connections.Add(con);
			return con;
		}

		/// <summary>
		/// Returns a task that yields until this <see cref="Signal"/> is invoked.
		/// </summary>
		/// <returns></returns>
		public Task<(T1, T2, T3, T4, T5)> Wait() {
			return Task.Run(() => {
				Waiter.Yield();
				return Waiter.Result;
			});
		}

		/// <summary>
		/// Asynchronously executes all <see cref="Connections"/>.
		/// </summary>
		/// <param name="objs"></param>
		/// <returns></returns>
		public async Task Invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5) {
			Waiter.Release(param1, param2, param3, param4, param5);
			foreach (var con in Connections) {
				await con.Fire(param1, param2, param3, param4, param5);
			}
		}

		internal void Disconnect(Connection<T1, T2, T3, T4, T5> con) {
			Connections.Remove(con);
		}

		internal void Disconnect(Func<T1, T2, T3, T4, T5, Task> method) {
			Connections.FirstOrDefault(con => con.Method == method)?.Disconnect();
		}

		/// <summary>
		/// Disconnects all event <see cref="Connection"/>s from this <see cref="Signal"/>.
		/// </summary>
		public void Dispose() {
			Connections.Clear();
		}

		public static Signal<T1, T2, T3, T4, T5> operator +(Signal<T1, T2, T3, T4, T5> left, Func<T1, T2, T3, T4, T5, Task> right) {
			left.Connect(right);
			return left;
		}

		public static Signal<T1, T2, T3, T4, T5> operator -(Signal<T1, T2, T3, T4, T5> left, Func<T1, T2, T3, T4, T5, Task> right) {
			left.Disconnect(right);
			return left;
		}
	}
}

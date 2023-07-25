#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;

namespace EtiBotCore.Clockwork {

	/// <summary>
	/// A utility class that helps to manage heartbeating from an active <see cref="DiscordClient"/>
	/// </summary>
	internal class HeartbeatClockwork : IDisposable {

		/// <summary>
		/// If a heartbeat acknowledge is not received this much time after sending it, the connection is assumed to have died.<para/>
		/// <strong>Default:</strong> 15000
		/// </summary>
		public int TimeoutMS { get; set; } = 15000;

		/// <summary>
		/// The time between a heartbeat and an acknowledge.
		/// </summary>
		public int LatencyMS { get; private set; } = 0;

		/// <summary>
		/// Whether or not a heartbeat has been sent but not yet acknowledged.
		/// </summary>
		public bool HasUnacknowledgedHeartbeat { get; private set; } = false;

		/// <summary>
		/// Provides the <see cref="CancellationToken"/> that stops this <see cref="HeartbeatClockwork"/>.
		/// </summary>
		private readonly ReusableCancellationTokenSource TokenSource = new ReusableCancellationTokenSource();

		/// <summary>
		/// A delegate method run when the <see cref="HeartbeatClockwork"/> times out.
		/// </summary>
		public delegate void TimedOut(TimeoutException exc);

		/// <summary>
		/// Fired when a heartbeat acknowledge does not come back after <see cref="TimeoutMS"/> ms
		/// </summary>
		public event TimedOut OnTimedOut;

		/// <summary>
		/// Whether or not this <see cref="HeartbeatClockwork"/> is running.
		/// </summary>
		public bool Running { get; private set; } = false;

		/// <summary>
		/// Whether or not this <see cref="HeartbeatClockwork"/> has timed out and must be restarted.
		/// </summary>
		public bool HasTimedOut { get; private set; } = false;

		/// <summary>
		/// Construct a new <see cref="HeartbeatClockwork"/>.
		/// </summary>
		/// <param name="startAutomatically">If <see langword="true"/>, the <see cref="StartChecking"/> method will be automatically called.</param>
		public HeartbeatClockwork(bool startAutomatically = false) {
			if (startAutomatically) StartChecking();
		}

		/// <summary>
		/// Should be called when a heartbeat is sent to Discord.
		/// </summary>
		public void Sent() {
			LatencyMS = 0;
			HasUnacknowledgedHeartbeat = true;
		}

		/// <summary>
		/// Should be called when a heartbeat is acknowledged from Discord.
		/// </summary>
		public void Acknowledged() {
			HasUnacknowledgedHeartbeat = false;
		}

		/// <summary>
		/// Stops the timer process and resets everything to its original state.
		/// </summary>
		public void StopChecking() {
			HasUnacknowledgedHeartbeat = false;
			Running = false;
			HasTimedOut = false;
			TokenSource.Cancel();
		}

		private void StopCheckingDueToTimeout() {
			HasUnacknowledgedHeartbeat = false;
			Running = false;
			HasTimedOut = true;
		}

		/// <summary>
		/// Starts the timer process.
		/// </summary>
		public void StartChecking() {
			if (Running) throw new InvalidOperationException("Cannot start checking - This clockwork is already running.");
			LatencyMS = 0;
			HasUnacknowledgedHeartbeat = false;
			HasTimedOut = false;
			Stopwatch watch = new Stopwatch();
			try {
				Task.Run(async () => {
					while (true) {
						watch.Start();
						await Task.Delay(100);
						watch.Stop();
						if (HasUnacknowledgedHeartbeat) {
							LatencyMS += (int)watch.ElapsedMilliseconds;
							if (LatencyMS > TimeoutMS) {
								StopCheckingDueToTimeout();
								OnTimedOut?.Invoke(new TimeoutException($"A heartbeat was not acknowledged within {TimeoutMS}ms."));
								TokenSource.Cancel();
								return;
							}
						}
						watch.Reset();
					}
				}, TokenSource.CurrentToken);
			} catch (OperationCanceledException) { }
		}

		/// <inheritdoc/>
		public void Dispose() {

			TokenSource.Cancel();
		}

	}
}

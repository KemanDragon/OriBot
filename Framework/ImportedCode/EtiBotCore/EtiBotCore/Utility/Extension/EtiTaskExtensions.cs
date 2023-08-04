using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EtiBotCore.Utility.Threading;
using EtiLogger.Logging;

namespace EtiBotCore.Utility.Extension {

	/// <summary>
	/// Provides a number of extensions to <see cref="Task"/> and <see cref="Task{TResult}"/>.
	/// </summary>
	public static class EtiTaskExtensions {

		/// <summary>
		/// Runs a <see cref="Task{TResult}"/>, or aborts if the time specified by <paramref name="timeout"/> passes, throwing a <see cref="TimeoutException"/>.
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="task"></param>
		/// <param name="timeout"></param>
		/// <param name="timeoutMessage"></param>
		/// <returns></returns>
		/// <exception cref="TimeoutException"></exception>
		public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout, string? timeoutMessage = null) {
			using CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource();
			Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
			if (completedTask == task) {
				timeoutCancellationTokenSource.Cancel();
				return await task;  // Very important in order to propagate exceptions.
			} else {
				throw new TimeoutException(timeoutMessage);
			}
		}

		/// <summary>
		/// Runs a <see cref="Task{TResult}"/>, or aborts if the time specified by <paramref name="timeoutMillis"/> passes, throwing a <see cref="TimeoutException"/>.
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="task"></param>
		/// <param name="timeoutMillis"></param>
		/// <param name="timeoutMessage"></param>
		/// <returns></returns>
		/// <exception cref="TimeoutException"></exception>
		public static Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, int timeoutMillis, string? timeoutMessage = null) => TimeoutAfter(task, TimeSpan.FromMilliseconds(timeoutMillis), timeoutMessage);

		/// <summary>
		/// Runs a <see cref="Task"/>, or aborts if the time specified by <paramref name="timeout"/> passes, throwing a <see cref="TimeoutException"/>.
		/// </summary>
		/// <param name="task"></param>
		/// <param name="timeout"></param>
		/// <param name="timeoutMessage"></param>
		/// <returns></returns>
		/// <exception cref="TimeoutException"></exception>
		public static async Task TimeoutAfter(this Task task, TimeSpan timeout, string? timeoutMessage = null) {
			using CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource();
			Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
			if (completedTask == task) {
				timeoutCancellationTokenSource.Cancel();
				await task;  // Very important in order to propagate exceptions.
			} else {
				throw new TimeoutException(timeoutMessage);
			}
		}


		/// <summary>
		/// Runs a <see cref="Task"/>, or aborts if the time specified by <paramref name="timeoutMillis"/> passes, throwing a <see cref="TimeoutException"/>.
		/// </summary>
		/// <param name="task"></param>
		/// <param name="timeoutMillis"></param>
		/// <param name="timeoutMessage"></param>
		/// <returns></returns>
		/// <exception cref="TimeoutException"></exception>
		public static Task TimeoutAfter(this Task task, int timeoutMillis, string? timeoutMessage = null) => TimeoutAfter(task, TimeSpan.FromMilliseconds(timeoutMillis), timeoutMessage);


		/// <summary>
		/// Launches a task with the given <see cref="ReusableCancellationTokenSource"/> to cancel it. This task should contain some infinite loop.
		/// </summary>
		/// <param name="taskFunc">A function that represents the async work.</param>
		/// <param name="errorLogger">Something to log exceptions to.</param>
		/// <param name="source">A <see cref="ReusableCancellationTokenSource"/> that provides a <see cref="CancellationToken"/> to stop the task.</param>
		public static Task Launch(Func<Task> taskFunc, Logger errorLogger, ReusableCancellationTokenSource source) {
			Exception? exc;
			try {
				return Task.Run(taskFunc, source.CurrentToken);
			} catch (OperationCanceledException oce) {
				exc = oce;
			} catch (Exception err) {
				exc = err;
				errorLogger.WriteException(exc);
			}
			return Task.FromException(exc);
		}

		/// <summary>
		/// Launches a task with the given <see cref="ReusableCancellationTokenSource"/> to cancel it. This task should contain some infinite loop.
		/// </summary>
		/// <param name="taskFunc">A function that represents the async work.</param>
		/// <param name="errorLogger">Something to log exceptions to.</param>
		/// <param name="source">A <see cref="ReusableCancellationTokenSource"/> that provides a <see cref="CancellationToken"/> to stop the task.</param>
		public static Task<T> Launch<T>(Func<Task<T>> taskFunc, Logger errorLogger, ReusableCancellationTokenSource source) {
			Exception? exc;
			try {
				return Task.Run(taskFunc, source.CurrentToken);
			} catch (OperationCanceledException oce) {
				exc = oce;
			} catch (Exception err) {
				exc = err;
				errorLogger.WriteException(exc);
			}
			return Task.FromException<T>(exc);
		}


		/// <summary>
		/// Synchronously runs the given <see cref="Task{TResult}"/> on the current thread (effectively stripping away the <see cref="Task"/> part)<para/>
		/// Additionally, this will propagate any exceptions raised in this task.
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="task"></param>
		/// <returns></returns>
		/// <exception cref="AggregateException">Any exceptions raised in the task.</exception>
		[Obsolete("don't cause deadlocks ffs", true)]
		public static TResult RunSync<TResult>(this Task<TResult> task) {
			/*
			TResult result = task.GetAwaiter().GetResult();
			if (task.IsFaulted) ExceptionDispatchInfo.Capture(task.Exception!).Throw();
			return result;
			*/
			return RunSync(() => task);
		}

		/// <summary>
		/// Synchronously runs the given <see cref="Task"/> on the current thread (effectively stripping away the <see cref="Task"/> part)<para/>
		/// Additionally, this will propagate any exceptions raised in this task.
		/// </summary>
		/// <param name="task"></param>
		/// <returns></returns>
		/// <exception cref="AggregateException">Any exceptions raised in the task.</exception>
		[Obsolete("don't cause deadlocks ffs", true)]
		public static void RunSync(this Task task) {
			/*
			task.GetAwaiter().GetResult();
			if (task.IsFaulted) ExceptionDispatchInfo.Capture(task.Exception!).Throw();
			*/
			RunSync(() => task);
		}

		/// <summary>
		/// Executes an <see langword="async"/> <see cref="Task"/> synchronously.
		/// </summary>
		/// <param name="task"><see cref="Task"/> to execute</param>
		public static void RunSync(Func<Task> task) {
			var oldContext = SynchronizationContext.Current;
			var synch = new ExclusiveSynchronizationContext();
			SynchronizationContext.SetSynchronizationContext(synch);
			synch.Post(async _ => {
				try {
					await task();
				} catch (Exception e) {
					synch.InnerException = e;
					throw;
				} finally {
					synch.EndMessageLoop();
				}
			}, null);
			synch.BeginMessageLoop();

			SynchronizationContext.SetSynchronizationContext(oldContext);
		}

		/// <summary>
		/// Executes an <see langword="async"/> <see cref="Task{TResult}"/> method which has a <typeparamref name="TResult"/> return type synchronously
		/// </summary>
		/// <typeparam name="TResult">Return Type</typeparam>
		/// <param name="task"><see cref="Task{TResult}"/> to execute</param>
		/// <returns></returns>
		public static TResult RunSync<TResult>(Func<Task<TResult>> task) {
			var oldContext = SynchronizationContext.Current;
			var synch = new ExclusiveSynchronizationContext();
			SynchronizationContext.SetSynchronizationContext(synch);
			TResult ret = default;
			synch.Post(async _ => {
				try {
					ret = await task();
				} catch (Exception e) {
					synch.InnerException = e;
					throw;
				} finally {
					synch.EndMessageLoop();
				}
			}, null);
			synch.BeginMessageLoop();
			SynchronizationContext.SetSynchronizationContext(oldContext);
			return ret!;
		}

		private class ExclusiveSynchronizationContext : SynchronizationContext {
			private bool Done;
			public Exception? InnerException { get; set; }
			private readonly AutoResetEvent WorkItemsAreWaiting = new AutoResetEvent(false);
			private readonly Queue<Tuple<SendOrPostCallback, object?>> CurrentItems = new Queue<Tuple<SendOrPostCallback, object?>>();

			public override void Send(SendOrPostCallback d, object? state) {
				throw new NotSupportedException("We cannot send to our same thread");
			}

			public override void Post(SendOrPostCallback d, object? state) {
				lock (CurrentItems) {
					CurrentItems.Enqueue(Tuple.Create(d, state));
				}
				WorkItemsAreWaiting.Set();
			}

			public void EndMessageLoop() {
				Post(_ => Done = true, null);
			}

			public void BeginMessageLoop() {
				while (!Done) {
					Tuple<SendOrPostCallback, object?>? task = null;
					lock (CurrentItems) {
						if (CurrentItems.Count > 0) {
							task = CurrentItems.Dequeue();
						}
					}
					if (task != null) {
						task.Item1(task.Item2);
						if (InnerException != null) // the method threw an exeption
						{
							throw new AggregateException("AsyncHelpers.Run method threw an exception.", InnerException);
						}
					} else {
						WorkItemsAreWaiting.WaitOne();
					}
				}
			}

			public override SynchronizationContext CreateCopy() {
				return this;
			}
		}
	}
}

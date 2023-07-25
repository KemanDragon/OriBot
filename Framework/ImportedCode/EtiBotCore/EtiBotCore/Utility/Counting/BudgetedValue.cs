using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Utility.Threading;

namespace EtiBotCore.Utility.Counting {

	/// <summary>
	/// A value with a limited quantity. These items are restored on an as-used basis.
	/// </summary>
	public class BudgetedValue {
		
		/// <summary>
		/// The amount of time it takes for the values value to be restored.
		/// </summary>
		public int RestoreTimeMillis { get; }

		/// <summary>
		/// The total amount of values possible.
		/// </summary>
		public int Size { get; }

		/// <summary>
		/// The amount of values remaining.
		/// </summary>
		public int Remaining { get; private set; }

		/// <summary>
		/// <see langword="true"/> if there are no values remaining, and <see langword="false"/> if there are.
		/// </summary>
		public bool Depleted => Remaining == 0;

		/// <summary>
		/// A resuable source for <see cref="CancellationToken"/>s used to stop the timer.
		/// </summary>
		protected ReusableCancellationTokenSource Source { get; } = new ReusableCancellationTokenSource();

		/// <summary>
		/// A delegate used when a value is restored.
		/// </summary>
		public delegate Task Restored();

		/// <summary>
		/// An event that fires when a value taken from the budget has been restored.
		/// </summary>
		public event Restored? OnRestored;

		/// <summary>
		/// Whether or not the timer is live.
		/// </summary>
		protected bool Live { get; set; } = false;

		/// <summary>
		/// Construct a new <see cref="BulkBudgetedValue"/> that will restore the values every <paramref name="restoreTimeMillis"/> milliseconds, and has a budget of <paramref name="objectBudget"/> values.<para/>
		/// This will not automatically start the timer. Once the timer is started, the system will loop until it is stopped, setting <see cref="Remaining"/> to <see cref="Size"/> (<paramref name="objectBudget"/>) once every <see cref="RestoreTimeMillis"/> milliseconds.
		/// </summary>
		/// <param name="restoreTimeMillis">The amount of time it takes for <see cref="Remaining"/> to be replenished.</param>
		/// <param name="objectBudget">The amount of values that can be taken at once.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="restoreTimeMillis"/> or <paramref name="objectBudget"/> are less than or equal to zero.</exception>
		public BudgetedValue(int restoreTimeMillis, int objectBudget) {
			if (restoreTimeMillis <= 0) throw new ArgumentOutOfRangeException(nameof(restoreTimeMillis));
			if (objectBudget <= 0) throw new ArgumentOutOfRangeException(nameof(objectBudget));
			Remaining = objectBudget;
			RestoreTimeMillis = restoreTimeMillis;
			Size = objectBudget;
		}

		/// <summary>
		/// Decrements <see cref="Remaining"/>. Throws a <see cref="BudgetExceededException"/> if <see cref="Remaining"/> == 0.<para/>
		/// Awaiting this <see cref="Task"/> will delay until the value that was taken is restored.
		/// </summary>
		/// <exception cref="BudgetExceededException">If <see cref="Remaining"/> == 0</exception>
		public Task Decrement() {
			if (Depleted) throw new BudgetExceededException();
			try {
				return Task.Run(async () => {
					Remaining--;
					await Task.Delay(RestoreTimeMillis, Source.CurrentToken);
					Remaining++;
					if (Remaining > Size) throw new InvalidOperationException("A decrement restore task caused Remaining to be greater than Size!");
					// ^ Error for me as a developer. Should not be documented as throwable.
					Task? restoreEvt = OnRestored?.Invoke();
					if (restoreEvt != null && !restoreEvt.IsCompleted) await restoreEvt;
				}, Source.CurrentToken);
			} catch (OperationCanceledException) { } // This is OK.

			return Task.CompletedTask; // This will never actually run.
		}

		/// <summary>
		/// Resets <see cref="Remaining"/> to <see cref="Size"/> and cancels all ongoing restoration tasks.
		/// </summary>
		public void Reset() {
			Live = false;
			Source.Cancel();
			Remaining = Size;
		}
	}
}

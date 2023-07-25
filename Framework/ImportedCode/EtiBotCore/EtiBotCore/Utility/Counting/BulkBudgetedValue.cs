using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;
using EtiBotCore.Exceptions.Marshalling;

namespace EtiBotCore.Utility.Counting {

	/// <summary>
	/// A value with a limited quantity. These items are simultaneously restored after a given time.
	/// </summary>
	public class BulkBudgetedValue {

		/// <summary>
		/// The current epoch in milliseconds.
		/// </summary>
		public static long Epoch => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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
		/// An event that will fire every time the budget is restored.
		/// </summary>
		protected ManualResetEventSlim RestoredEvent = new ManualResetEventSlim();

		/// <summary>
		/// The epoch of the next restoration.
		/// </summary>
		protected long NextRestoreOccursAt = 0;

		/// <summary>
		/// Construct a new <see cref="BulkBudgetedValue"/> that will restore the values every <paramref name="restoreTimeMillis"/> milliseconds, and has a budget of <paramref name="objectBudget"/> values.<para/>
		/// </summary>
		/// <param name="restoreTimeMillis">The amount of time it takes for <see cref="Remaining"/> to be replenished.</param>
		/// <param name="objectBudget">The amount of values that can be taken at once.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="restoreTimeMillis"/> or <paramref name="objectBudget"/> are less than or equal to zero.</exception>
		public BulkBudgetedValue(int restoreTimeMillis, int objectBudget) {
			if (restoreTimeMillis <= 0) throw new ArgumentOutOfRangeException(nameof(restoreTimeMillis));
			if (objectBudget <= 0) throw new ArgumentOutOfRangeException(nameof(objectBudget));
			Remaining = objectBudget;
			RestoreTimeMillis = restoreTimeMillis;
			Size = objectBudget;
			NextRestoreOccursAt = Epoch + restoreTimeMillis;
		}

		/// <summary>
		/// Decrements <see cref="Remaining"/>. Throws a <see cref="BudgetExceededException"/> if <see cref="Remaining"/> == 0.
		/// </summary>
		/// <exception cref="BudgetExceededException">If <see cref="Remaining"/> == 0</exception>
		public void Spend() {
			while (Epoch > NextRestoreOccursAt) {
				// Using while will ensure next is always the next
				NextRestoreOccursAt += RestoreTimeMillis;
				Remaining = Size;
			}
			if (Depleted) throw new BudgetExceededException();
			Remaining--;
		}

		/// <summary>
		/// Returns a <see cref="Task"/> that delays until the next occurrence of <see cref="Remaining"/> being reset to <see cref="Size"/>, assuming <see cref="Remaining"/> is 0.
		/// </summary>
		public Task WaitForNextRestore() {
			if (Remaining == 0 && NextRestoreOccursAt > Epoch) {
				return Task.Delay((int)(NextRestoreOccursAt - Epoch));
			}
			return Task.CompletedTask;
		}
	}
}

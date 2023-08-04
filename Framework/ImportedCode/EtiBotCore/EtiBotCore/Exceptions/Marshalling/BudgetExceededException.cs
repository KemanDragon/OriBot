using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Utility.Counting;

namespace EtiBotCore.Exceptions.Marshalling {

	/// <summary>
	/// An exception that is thrown when a <see cref="BulkBudgetedValue"/> has its <see cref="BulkBudgetedValue.Spend"/> method called but also has its <see cref="BulkBudgetedValue.Remaining"/> = 0
	/// </summary>
	public class BudgetExceededException : Exception {

		/// <summary>
		/// Construct a new <see cref="BudgetExceededException"/> with the generic message: <c>No more values are available!</c>
		/// </summary>
		public BudgetExceededException() : this("No more values are available!") { }

		/// <summary>
		/// Construct a new <see cref="BudgetExceededException"/> with a custom message.
		/// </summary>
		public BudgetExceededException(string message) : base(message) { }

	}
}

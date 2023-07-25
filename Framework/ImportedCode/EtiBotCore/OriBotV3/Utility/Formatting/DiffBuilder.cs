using System;
using System.Collections.Generic;
using System.Text;

namespace OldOriBot.Utility.Formatting {

	/// <summary>
	/// Constructs diff lists.
	/// </summary>
	public class DiffBuilder {

		private readonly StringBuilder Diff = new StringBuilder("```diff\n");

		private bool Finalized = false;

		/// <summary>
		/// Appends something to this <see cref="DiffBuilder"/> as an addition.
		/// </summary>
		/// <param name="thing"></param>
		public void Added(string thing) {
			if (Finalized) throw new InvalidOperationException($"This {nameof(DiffBuilder)} has been finalized.");
			Diff.AppendLine("+ " + thing);
		}

		/// <summary>
		/// Appends something to this <see cref="DiffBuilder"/> as a change.
		/// </summary>
		/// <param name="thing"></param>
		public void Changed(string thing) {
			if (Finalized) throw new InvalidOperationException($"This {nameof(DiffBuilder)} has been finalized.");
			Diff.AppendLine("* " + thing);
		}

		/// <summary>
		/// Appends something to this <see cref="DiffBuilder"/> as a removal.
		/// </summary>
		/// <param name="thing"></param>
		public void Removed(string thing) {
			if (Finalized) throw new InvalidOperationException($"This {nameof(DiffBuilder)} has been finalized.");
			Diff.AppendLine("- " + thing);
		}

		/// <summary>
		/// Returns the formatted diff code block and locks this object.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (!Finalized) {
				return Diff.ToString() + "```";
			}
			return Diff.ToString();
		}

		/// <summary>
		/// Finalizes this object, caching its return value.
		/// </summary>
		public void Lock() {
			if (!Finalized) {
				Diff.Append("```");
				Finalized = true;
			}
		}

	}
}

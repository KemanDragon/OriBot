using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtiBotCore.Utility.Threading {

	/// <summary>
	/// A variant of <see cref="CancellationTokenSource"/> that provides tokens as needed without needing to be reconstructed.
	/// </summary>
	public class ReusableCancellationTokenSource : IDisposable {

		/// <summary>
		/// When a <see cref="CancellationTokenSource"/> is cancelled, it becomes useless and must be disposed of. This is the latest available <see cref="CancellationTokenSource"/> that can provide a new token.
		/// </summary>
		private CancellationTokenSource? LatestSource = new CancellationTokenSource();

		/// <summary>
		/// Returns the current <see cref="CancellationToken"/> that is available right now.
		/// </summary>
		/// <exception cref="ObjectDisposedException"/>
		public CancellationToken CurrentToken => LatestSource!.Token;

		/// <summary>
		/// Cancels the underlying <see cref="CancellationTokenSource"/> and then creates a new instance, updating the value of <see cref="CurrentToken"/> in the process.
		/// </summary>
		/// <exception cref="ObjectDisposedException"/>
		public void Cancel() {
			LatestSource!.Cancel();
			LatestSource = new CancellationTokenSource();
		}

		/// <inheritdoc/>
		public void Dispose() {
			LatestSource!.Dispose();
			LatestSource = null;
		}

	}
}

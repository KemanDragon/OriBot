using System;
using System.Collections.Generic;
using System.Text;

namespace OldOriBot.Exceptions {

	/// <summary>
	/// Setting an exception's <see cref="Exception.InnerException"/> to this will cause it to not show in the bot console.
	/// </summary>
	public sealed class NoThrowDummyException : Exception { }
}

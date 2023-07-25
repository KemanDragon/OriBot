using System;
using System.Collections.Generic;
using System.Text;
using OldOriBot.Interaction;

namespace OldOriBot.Exceptions {

	/// <summary>
	/// An error for when a command fails to run.
	/// </summary>
	public class CommandException : Exception {

		/// <summary>
		/// The <see cref="Command"/> that raised this <see cref="CommandException"/>
		/// </summary>
		public Command Cause { get; }

		public CommandException(Command source, string message) : base(message) {
			Cause = source;
		}

		public CommandException(Command source, Exception inner) : base(inner.Message, inner) {
			Cause = source;
		}

		public CommandException(Command source, string message, Exception inner) : base(message, inner) {
			Cause = source;
		}

	}
}

using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.DiscordObjects;

namespace EtiBotCore.Exceptions.Marshalling {

	/// <summary>
	/// Thrown when this object is temporarily unavailable for any reason. Contrary to <see cref="ObjectDeletedException"/>, this object may return to a state where this exception would no longer be thrown.
	/// </summary>
	public class ObjectUnavailableException : Exception {

		/// <summary>
		/// The object that raised this exception.
		/// </summary>
		public DiscordObject Object { get; }

		/// <inheritdoc/>
		public ObjectUnavailableException(DiscordObject from) : this(from, "This object is unavailable.") { }

		/// <inheritdoc/>
		public ObjectUnavailableException(DiscordObject from, string message) : base(message) {
			Object = from;
		}

	}
}

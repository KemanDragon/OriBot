using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.DiscordObjects;

namespace EtiBotCore.Exceptions.Marshalling {

	/// <summary>
	/// An exception that is thrown if a <see cref="DiscordObject"/> is deleted and you attempt to edit it.
	/// </summary>
	public class ObjectDeletedException : Exception {

		/// <summary>
		/// The <see cref="DiscordObject"/> that raised this exception.
		/// </summary>
		public DiscordObject Origin { get; }

		/// <summary>
		/// Construct a new <see cref="ObjectDeletedException"/> and set <see cref="Origin"/> to the given object.
		/// </summary>
		/// <param name="source">The object that raised this exception</param>
		public ObjectDeletedException(DiscordObject source) : base("This DiscordObject has been deleted and cannot be edited - it exists only for reference of its properties prior to deletion.") {
			Origin = source;
		}

	}
}

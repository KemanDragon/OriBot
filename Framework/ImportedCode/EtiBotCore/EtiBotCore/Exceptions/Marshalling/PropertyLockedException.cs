using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace EtiBotCore.Exceptions.Marshalling {

	/// <summary>
	/// An exception raised when a property is locked and its <see langword="set"/> method is used.
	/// </summary>
	public class PropertyLockedException : Exception {

		/// <summary>
		/// Construct a new <see cref="PropertyLockedException"/> with the default message: <c>The NAME_HERE property is locked! Did you forget to call BeginChanges()</c>
		/// </summary>
		/// <param name="propName">The name of the property. By default, rather than being null, it is the name of the member that called the ctor.</param>
		public PropertyLockedException([CallerMemberName] string? propName = null) : base($"The {propName ?? "UNDEFINED"} property is locked! Did you forget to call BeginChanges()?") { }


	}
}

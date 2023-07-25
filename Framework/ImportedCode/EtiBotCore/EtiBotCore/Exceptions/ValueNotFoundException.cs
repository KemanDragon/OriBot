using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Exceptions {

	/// <summary>
	/// Similar to <see cref="KeyNotFoundException"/>, but is used in the KeyOf extension method.
	/// </summary>
	internal class ValueNotFoundException : KeyNotFoundException {

		/// <summary>
		/// Construct a <see cref="ValueNotFoundException"/> with a default message of <c>The given value was not present in the dictionary.</c>
		/// </summary>
		public ValueNotFoundException() : this("The given value was not present in the dictionary.") { }

		/// <summary>
		/// Construct a <see cref="ValueNotFoundException"/> with the given message.
		/// </summary>
		/// <param name="message">The message to include with the exception.</param>
		public ValueNotFoundException(string message) : base(message) { }
	
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Events {

	/// <summary>
	/// This event derives from another and should not be registered as it shares an ID and usage context.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class IgnoreEventAttribute : Attribute {

		/// <summary>
		/// Construct a new <see cref="IgnoreEventAttribute"/>.
		/// </summary>
		public IgnoreEventAttribute() { }
	}
}

using EtiBotCore.Data.JsonConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Utility.Attributes {

	/// <summary>
	/// Tells this enum to convert by serializing its elements' names instead of their numeric values when handled by <see cref="EnumConverter"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
	public sealed class ConvertEnumByNameAttribute : Attribute { }
}

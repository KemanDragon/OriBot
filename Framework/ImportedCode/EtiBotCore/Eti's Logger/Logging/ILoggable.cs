using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiLogger.Logging {

	/// <summary>
	/// Represents a class that has a special <see cref="ToMessage"/> method designed for use in <see cref="Logger"/>'s formatting system.
	/// </summary>
	public interface ILoggable {

		/// <summary>
		/// Translate this object into a string that uses <see cref="Logger"/>'s formatting system.
		/// </summary>
		/// <returns></returns>
		LogMessage ToMessage();

	}
}

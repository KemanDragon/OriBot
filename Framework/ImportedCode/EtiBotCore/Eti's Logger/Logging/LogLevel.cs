using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiLogger.Logging {

	/// <summary>
	/// A logging level, which can be used to filter between different log entry types.
	/// </summary>
	public enum LogLevel {

		/// <summary>
		/// A standard log entry message.
		/// </summary>
		Info = 0,

		/// <summary>
		/// A log entry for debugging purposes.
		/// </summary>
		Debug = 1,

		/// <summary>
		/// A log entry for trace debugging, which keeps track of code flow.
		/// </summary>
		Trace = 2

	}
}

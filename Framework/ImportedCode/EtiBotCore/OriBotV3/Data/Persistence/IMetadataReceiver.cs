using System;
using System.Collections.Generic;
using System.Text;

namespace OldOriBot.Data.Persistence {

	/// <summary>
	/// Allows an instance of <see cref="IBinaryReadWrite"/> to receive data before the read or write operation is complete.
	/// </summary>
	public interface IMetadataReceiver : IBinaryReadWrite {

		/// <summary>
		/// Receive arbtirary data before a read or write operation.
		/// </summary>
		/// <param name="data"></param>
		void ReceiveMetadata(params object[] data);

	}
}

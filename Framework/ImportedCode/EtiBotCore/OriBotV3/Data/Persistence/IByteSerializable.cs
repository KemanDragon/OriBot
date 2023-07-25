using System;
using System.Collections.Generic;
using System.Text;

namespace OldOriBot.Data.Persistence {
	public interface IByteSerializable {

		/// <summary>
		/// Create a byte array representing this object.
		/// </summary>
		/// <returns></returns>
		byte[] ToBytes();

		/// <summary>
		/// Populate this object's data from a byte array. Returns the size of the object.
		/// </summary>
		int FromBytes(byte[] data);

	}
}

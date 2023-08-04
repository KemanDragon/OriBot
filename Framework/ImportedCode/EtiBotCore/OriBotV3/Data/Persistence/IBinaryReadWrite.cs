using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OldOriBot.Data.Persistence {

	/// <summary>
	/// Much like <see cref="IByteSerializable"/> but instead of dealing with byte arrays, it directly reads from or writes to a stream.
	/// </summary>
	public interface IBinaryReadWrite {

		/// <summary>
		/// Writes this object to the <see cref="BinaryWriter"/>.
		/// </summary>
		/// <param name="writer"></param>
		void Write(BinaryWriter writer);

		/// <summary>
		/// Populates this object's data from the <see cref="BinaryReader"/>.
		/// </summary>
		/// <param name="reader"></param>
		void Read(BinaryReader reader);

	}
}

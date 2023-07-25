using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.DiscordObjects;

namespace EtiBotCore.Exceptions {

	/// <summary>
	/// An exception used when a <see cref="IBackup"/>'s type is not the same as the type of object passed into it.
	/// </summary>
	class InvalidBackupTypeException : InvalidOperationException {

		/// <summary>
		/// 
		/// </summary>
		public InvalidBackupTypeException() : base("Invalid backup type!") { }

	}
}

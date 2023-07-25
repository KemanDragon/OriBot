using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Utility.Marshalling {

	/// <summary>
	/// A singleton object.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class Singleton<T> where T : new() {

		/// <summary>
		/// The singleton instance of this class.
		/// </summary>
		public static T Instance { get; } = new T();

	}
}

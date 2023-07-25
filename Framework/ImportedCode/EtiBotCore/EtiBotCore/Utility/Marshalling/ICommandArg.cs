#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Utility.Marshalling {

	/// <summary>
	/// Provides an interface that allows returning the given type from the given other type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface ICommandArg<out T> : ICommandArg where T : class, new() {

		/// <summary>
		/// Converts the given string (presumably a command argument) into a <strong>new instance</strong> (this does not populate this instance!) of <typeparamref name="T"/>, or null if it is not convertible.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="inContext"></param>
		/// <returns></returns>
		new T? From(string instance, object? inContext);

	}

	/// <summary>
	/// Provides an interface that allows returning an object from the given other type.
	/// </summary>
	public interface ICommandArg {

		/// <summary>
		/// Converts the given string (presumably a command argument) into an <strong>new instance</strong> (this does not populate this instance!) of this object, or null if it is not convertible.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="inContext"></param>
		/// <returns></returns>
		object? From(string instance, object? inContext);

	}
}

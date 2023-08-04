using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads;

namespace EtiBotCore.DiscordObjects {

	/// <summary>
	/// Represents an object that can be converted to and from a <see cref="PayloadDataObject"/>, as well as updated by a <see cref="PayloadDataObject"/>
	/// </summary>
	[Obsolete("You don't send payloads back to discord you fat retard", true)]
	public interface IPayloadConvertible {

		/// <summary>
		/// Whether or not this <see cref="IPayloadConvertible"/> can be updated by an incoming payload.
		/// </summary>
		/// <returns></returns>
		public bool IsUpdateable { get; }

		/// <summary>
		/// Converts this object to a <see cref="PayloadDataObject"/>
		/// </summary>
		/// <returns></returns>
		public PayloadDataObject ToPayload();

		/// <summary>
		/// Converts a <see cref="PayloadDataObject"/> into this type.
		/// </summary>
		/// <param name="obj"></param>
		public void FromPayload(PayloadDataObject obj);

		/// <summary>
		/// Tweaks the properties and/or fields of this object to reflect the fields of the given <see cref="PayloadDataObject"/>.
		/// </summary>
		/// <param name="obj"></param>
		public void Update(PayloadDataObject obj);

	}
}

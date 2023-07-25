using EtiBotCore.Data;
using EtiBotCore.Payloads.Events;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads {

	/// <summary>
	/// Treats this class as payload data, which means it will be an object in the data field of <see cref="Payload"/>.
	/// </summary>
	public abstract class PayloadDataObject {

		/// <summary>
		/// Calls <see cref="IEvent.GetEventName"/> if this <see cref="PayloadDataObject"/> implements <see cref="IEvent"/>, or <see langword="null"/> otherwise.
		/// </summary>
		/// <returns></returns>
		public string? GetEventName() {
			if (!HasCached) {
				if (GetType().Implements(typeof(IEvent))) {
					CachedEventName = PayloadEventRegistry.GetEventName(GetType());
				}
				HasCached = true;
			}
			return CachedEventName;
		}
		[JsonIgnore] private string? CachedEventName = null;
		[JsonIgnore] private bool HasCached = false;

		/// <summary>
		/// Converts this <see cref="PayloadDataObject"/> into its json string.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return JsonConvert.SerializeObject(this);
		}

		/// <inheritdoc cref="ToString"/>
		public string ToJson() => ToString();

	}

}

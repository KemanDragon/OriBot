using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace EtiBotCore.Exceptions {
	class ValueNotAllowedException : Exception {

		/// <summary>
		/// The feature this guild has that is preventing the property from being set to the desired value.
		/// </summary>
		public string LimitingFeature { get; }

		/// <summary>
		/// Throws this exception with a message: <c>$"The property {prop} cannot be set to {value} because this guild has the {limitingFeature} attribute/feature, which dictates that this value is not allowed."</c>
		/// </summary>
		/// <param name="limitingFeature"></param>
		/// <param name="value"></param>
		/// <param name="prop">Should not be manually set, this is calculated in runtime.</param>
		public ValueNotAllowedException(string limitingFeature, object? value, [CallerMemberName] string? prop = null) : base($"The property {prop} cannot be set to {value} because this guild has the {limitingFeature} attribute/feature, which dictates that this value is not allowed.") {
			LimitingFeature = limitingFeature;
		}

	}
}

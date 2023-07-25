using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace EtiBotCore.Exceptions.Marshalling {

	/// <summary>
	/// Thrown if something is set on a guild and the guild does not have that feature available to it.
	/// </summary>
	public class GuildFeatureNotAvailableException : Exception {

		/// <summary>
		/// The feature that is required to use this.
		/// </summary>
		public string RequiredFeature { get; }

		/// <summary>
		/// Construct a new <see cref="GuildFeatureNotAvailableException"/> from the given feature name, desired value, and property name (which will be automatically set)
		/// </summary>
		/// <param name="feature">The name of the feature e.g. COMMUNITY</param>
		/// <param name="value">The value that the user tried to set this to.</param>
		/// <param name="prop">The associated property name, which will be automatically populated, so don't set it.</param>
		public GuildFeatureNotAvailableException(string feature, object? value, [CallerMemberName] string? prop = null) : base($"Cannot set {prop} to this value ({value}) -- The guild does not have the {feature} attribute/feature!") {
			RequiredFeature = feature;
		}

	}
}

using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads.Data;

namespace EtiBotCore.Exceptions.Marshalling {


	/// <summary>
	/// An exception thrown when something is attempted to be performed without a valid intent.
	/// </summary>
	public class GatewayIntentNotEnabledException : InvalidOperationException {

		/// <summary>
		/// The <see cref="GatewayIntent"/> that is required to perform this operation.
		/// </summary>
		public GatewayIntent RequiredIntent { get; }

		/// <summary>
		/// An exception thrown when something is attempted to be performed without a valid intent.
		/// </summary>
		public GatewayIntentNotEnabledException(GatewayIntent intent) : base($"The {intent} gateway intent is not active! It must be enabled in order to access this system or feature.") {
			RequiredIntent = intent;
		}

	}
}

using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;

namespace EtiBotCore.Exceptions.Marshalling {

	/// <summary>
	/// An exception for when the bot does not have the permissions necessary to do something.
	/// </summary>
	public class InsufficientPermissionException : Exception {

		/// <inheritdoc/>
		public override string Message { get; }

		/// <summary>
		/// Whether or not <see cref="Message"/> is the stock <c>"Missing the necessary permissions required to perform this operation."</c>.
		/// </summary>
		public bool HasCustomMessage { get; }

		/// <summary>
		/// The permissions required to perform the associated operation.<para/>
		/// This will be <see langword="null"/> if this was thrown due to a reason not tied to <see cref="Permissions"/>, and instead a reason will be provided in <see cref="Exception.Message"/>.
		/// </summary>
		public Permissions? RequiredPermissions { get; }

		/// <summary>
		/// A formatted <see cref="string"/> that highlights the required permissions.<para/>
		/// This will be <see langword="null"/> if this was thrown due to a reason not tied to <see cref="Permissions"/>, and instead a reason will be provided in <see cref="Exception.Message"/>.
		/// </summary>
		public string? PermissionsAsString { get; }

		/// <summary>
		/// Constructs a new <see cref="InsufficientPermissionException"/> with a default message <c>Missing the permissions required to perform this operation.</c>
		/// </summary>
		/// <param name="requiredPerms"></param>
		public InsufficientPermissionException(Permissions requiredPerms) : base() {
			RequiredPermissions = requiredPerms;
			PermissionsAsString = requiredPerms.NameOfEach();
			Message = "Missing the necessary permissions required to perform this operation.";
			HasCustomMessage = false;
		}

		/// <summary>
		/// Constructs a new <see cref="InsufficientPermissionException"/> with the given message, and sets <see cref="RequiredPermissions"/> and <see cref="PermissionsAsString"/> to <see langword="null"/>.
		/// </summary>
		/// <param name="reason"></param>
		public InsufficientPermissionException(string reason) : base() {
			RequiredPermissions = null;
			PermissionsAsString = null;
			Message = reason;
			HasCustomMessage = true;
		}

	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace OldOriBot.Utility.Extensions {

	// TODO: Special argument class?

	/// <summary>
	/// An extension that provides a means of splitting a string into arguments via shell32.dll
	/// </summary>
	public static class Shell32ArgumentExtension {
		[DllImport("shell32.dll", SetLastError = true)]
		private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

		/// <summary>
		/// Takes in a raw string and converts it to an array of arguments via shell32.dll. As such, this behaves identically to a command prompt.<para/>
		/// Arguments within quotes will be treated as a single argument (containing spaces, optionally). Quotes must be escaped to be used literally, spaces separate args, etc.
		/// </summary>
		/// <param name="text">The raw string that should be split into command line args.</param>
		/// <returns>An array of string arguments.</returns>
		public static string[] SplitArgs(this string text) {

			// Special case: Some devices (phones and macbooks) replace quotes with fancy variants. Fix this.
			text = text.ReplaceQuotationMarks();

			IntPtr argPtr = CommandLineToArgvW(text, out int argc);
			if (argPtr == IntPtr.Zero)
				throw new Win32Exception();
			try {
				string[] args = new string[argc];
				for (int argIndex = 0; argIndex < args.Length; argIndex++) {
					IntPtr strPtr = Marshal.ReadIntPtr(argPtr, argIndex * IntPtr.Size);
					args[argIndex] = Marshal.PtrToStringUni(strPtr);
				}

				return args;
			} finally {
				Marshal.FreeHGlobal(argPtr);
			}
		}
	}

}

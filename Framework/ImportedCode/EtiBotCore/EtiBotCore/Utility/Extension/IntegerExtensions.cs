using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EtiBotCore.Utility.Extension {

	/// <summary>
	/// Provides extensions to integer classes to write them to streams in big endian.
	/// </summary>
	public static class IntegerExtensions {

		/// <summary>
		/// use this to lose 8 intelligene
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		[Obsolete("THATS THE JOKE YA HALFWIT", true)] public static byte[] ToBigEndian(this byte b) => throw new NotImplementedException();

		private static byte[] SwapIfSysIsLittle(byte[] data) {
			if (BitConverter.IsLittleEndian) {
				return data.Reverse().ToArray();
			}
			return data;
		}


		/// <summary>
		/// Converts the given value to a big endian array of bytes.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static byte[] ToBigEndian(this short s) {
			return SwapIfSysIsLittle(BitConverter.GetBytes(s));
		}

		/// <summary>
		/// Converts the given value to a big endian array of bytes.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static byte[] ToBigEndian(this ushort s) {
			return SwapIfSysIsLittle(BitConverter.GetBytes(s));
		}

		/// <summary>
		/// Converts the given value to a big endian array of bytes.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static byte[] ToBigEndian(this int s) {
			return SwapIfSysIsLittle(BitConverter.GetBytes(s));
		}

		/// <summary>
		/// Converts the given value to a big endian array of bytes.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static byte[] ToBigEndian(this uint s) {
			return SwapIfSysIsLittle(BitConverter.GetBytes(s));
		}

		/// <summary>
		/// Converts the given value to a big endian array of bytes.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static byte[] ToBigEndian(this long s) {
			return SwapIfSysIsLittle(BitConverter.GetBytes(s));
		}

		/// <summary>
		/// Converts the given value to a big endian array of bytes.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static byte[] ToBigEndian(this ulong s) {
			return SwapIfSysIsLittle(BitConverter.GetBytes(s));
		}



	}
}

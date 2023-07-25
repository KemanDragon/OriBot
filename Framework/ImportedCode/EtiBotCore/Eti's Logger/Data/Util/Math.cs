using System;
using System.Collections.Generic;
using System.Text;

namespace EtiLogger.Data.Util {

	/// <summary>
	/// Provides some specific math methods.
	/// </summary>
	public static class Math {

		/// <summary>
		/// Linearly interpolates start to goal based on alpha percent. Rounds the result. Unclamped.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="goal"></param>
		/// <param name="alpha"></param>
		/// <returns></returns>
		public static byte LerpByte(byte start, byte goal, float alpha) {
			if (goal == start) return start;
			if (alpha == 0) return start;
			if (alpha == 1) return goal;
			return (byte)System.Math.Round(((goal - start) * alpha) + start);
		}

	}
}

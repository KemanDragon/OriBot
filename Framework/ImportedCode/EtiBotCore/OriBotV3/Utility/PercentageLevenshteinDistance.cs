using System;
using System.Collections.Generic;
using System.Text;
using Fastenshtein;

namespace OldOriBot.Utility {
	public static class PercentageLevenshteinDistance {

		/*
		The Levenshtein distance has several simple upper and lower bounds. These include:
		It is at least the difference of the sizes of the two strings.
		It is at most the length of the longer string.
		It is zero if and only if the strings are equal.
		If the strings are the same size, the Hamming distance is an upper bound on the Levenshtein distance. The Hamming distance is the number of positions at which the corresponding symbols in the two strings are different.
		The Levenshtein distance between two strings is no greater than the sum of their Levenshtein distances from a third string (triangle inequality).
		*/

		/// <summary>
		/// Returns how similar these two strings are as a percentage rather than as an integer value.
		/// </summary>
		/// <param name="alpha"></param>
		/// <param name="bravo"></param>
		/// <returns>A percentage where 1 means the strings are identical. This will never return zero, as there is always substitution that can be used to transform one string into another.</returns>
		public static double GetSimilarityPercentage(string alpha, string bravo) {
			if (alpha == bravo) return 1;
			int sizeDiff = Math.Abs(alpha.Length - bravo.Length);
			int longerDistance = Math.Max(alpha.Length, bravo.Length);
			int d = Levenshtein.Distance(alpha, bravo);
			// at least the difference of the string sizes
			d -= sizeDiff;
			longerDistance -= sizeDiff;
			return 1D - ((double)d / longerDistance);
		}

	}
}

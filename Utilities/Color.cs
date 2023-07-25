using System;

namespace OriBot.Utilities
{
    /// <summary>
    /// RGB Colors for terminals that support 24-bit colors
    /// </summary>
    public class Color
    {
        /* ********** **
        ** ATTRIBUTES **
        ** ********** */
        private String ansiCodeTemplate = "\u001b[38;2;r;g;bm";
        private static String ansiCodeReset = "\u001b[0m";
        private String ansiCode;

        /* *********** **
        ** CONSTRUCTOR **
        ** *********** */

        public Color(int r, int g, int b)
        {
            // TODO: check if terminal supports true color
            ansiCode = ansiCodeTemplate.Replace("r", r.ToString()).Replace("g", g.ToString()).Replace("b", b.ToString());
        }

        /* ******* **
        ** METHODS **
        ** ******* */

        /// <summary>
        /// Returns an ANSI Code to reset the terminal's colors
        /// </summary>
        /// <returns>ANSI Code</returns>
        public static String Reset()
        {
            return ansiCodeReset;
        }

        public override String ToString()
        {
            return ansiCode;
        }
    }
}
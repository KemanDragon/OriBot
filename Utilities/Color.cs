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
            if (SupportsColor())
                ansiCode = ansiCodeTemplate.Replace("r", r.ToString()).Replace("g", g.ToString()).Replace("b", b.ToString());
            else
                ansiCode = "";
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

        /// <summary>
        /// Checks whether the terminal supports color
        /// </summary>
        /// <returns>bool: Whether color is supported</returns>
        private static bool SupportsColor()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return true; // Windows supports color by default
            }
            else
            {
                string term = Environment.GetEnvironmentVariable("TERM");
                if (!string.IsNullOrEmpty(term) && term.Contains("color", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public override String ToString()
        {
            return ansiCode;
        }
    }
}
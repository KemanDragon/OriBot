using System;

namespace Utilities
{
    public class Color 
    {
        private String ansiCodeTemplate = "\u001b[38;2;r;g;bm";
        private String ansiCode;

        public Color(int r, int g, int b)
        {
            ansiCode = ansiCodeTemplate.Replace("r", r.ToString()).Replace("g", g.ToString()).Replace("b", b.ToString()); 
        }

        public override String ToString()
        {
            return ansiCode;
        }
    }
}


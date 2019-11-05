using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleCLCL.Utils
{
    class TextHelper
    {
        public static string MinimizeWhiteSpaces(string input)
        {
            // Remove everything except newline
            var r = Regex.Replace(input, @"(?:(?![\n\r])\s)+", " ");

            // Trim each line (remove spaces at beginning and end)
            r = string.Join("\n", r.Split('\n').Select(s => s.Trim()));

            // replace multiple spaces with max one space
            return Regex.Replace(r, @"\n((?:(?![\n\r])\s)+)", " ");
        }
    }
}

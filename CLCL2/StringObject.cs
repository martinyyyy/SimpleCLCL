using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleCLCL
{
    public class StringObject
    {
        public string value { get; set; }
        public string shortValue
        {
            get
            {
                String v = Regex.Replace(value, @"\t|\n|\r", "");
                v = v.Trim();
                return v;
            }
        }

        public bool isShort
        {
            get
            {
                return value.Length > 40;
            }
        }
    }
}

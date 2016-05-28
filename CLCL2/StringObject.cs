using System.Text.RegularExpressions;

namespace SimpleCLCL
{
    public class StringObject
    {
        public string Value { get; set; }
        public string ShortValue
        {
            get
            {
                var value = Regex.Replace(Value, @"\t|\n|\r", "");
                value = value.Trim();
                return value;
            }
        }

        public bool IsPinned { get; set; }

        public bool IsShort => Value.Length > 40;
    }
}

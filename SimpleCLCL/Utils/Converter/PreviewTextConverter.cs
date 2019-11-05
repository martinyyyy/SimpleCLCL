using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SimpleCLCL.Utils.Converter
{
    public class PreviewTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                s = TextHelper.MinimizeWhiteSpaces(s);
                s = Regex.Replace(s, @"\t|\r", "");
                s = Regex.Replace(s, @"\n", " ").Trim();
                return s;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCLCL.Utils
{
    class SettingsHelper
    {
        public static List<string> Load()
        {
            var settings = SimpleCLCL.Properties.Settings.Default.History ?? new StringCollection();
            return settings.Cast<string>().ToList();
        }

        public static void Save(List<string> items)
        {
            SimpleCLCL.Properties.Settings.Default.History = new StringCollection();
            foreach (var item in items)
                SimpleCLCL.Properties.Settings.Default.History.Add(item);

            SimpleCLCL.Properties.Settings.Default.Save();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SimpleCLCL.Annotations;
using SimpleCLCL.Utils;

namespace SimpleCLCL.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region INotifyHandler
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public IEnumerable<string> FilteredEntrys => _entrys.Where(x => x.ToLower().Contains(Search.ToLower()));
        private string _search = string.Empty;
        private readonly List<string> _entrys = new List<string>();

        public string Search
        {
            get => _search;
            set
            {
                if (value == _search) return;
                _search = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredEntrys));
            }
        }

        public MainViewModel()
        {
            for (int i = 0; i < 100; i++) Add("SACK" + i);
        }

        public void Add(string entry)
        { 
            _entrys.Remove(entry);
            _entrys.Insert(0,entry);

            OnPropertyChanged(nameof(FilteredEntrys));
        }

        public void AddAll(IEnumerable<string> range) => _entrys.AddRange(range);
        public void Remove(string entry) => _entrys.Remove(entry);

        public void ClipboardTextChanged(object sender, ClipboardTextChangedEventArgs e)
        {
            Add(e.Text);
        }
    }
}

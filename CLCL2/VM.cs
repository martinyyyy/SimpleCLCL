using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SimpleCLCL
{
    public class VM : INotifyPropertyChanged
    {
        string _currentSearch = "";
        public string currentSearch
        {
            get
            {
                return _currentSearch;
            }
            set
            {
                if (_currentSearch != value)
                {
                    _currentSearch = value;
                    RaisePropertyChanged("currentSearch");
                    RaisePropertyChanged("clipboardEntrys");
                    RaisePropertyChanged("searchVisible");
                }
            }
        }

        public bool searchVisible
        {
            get
            {
                return currentSearch != "";
            }
        }

        ObservableCollection<StringObject> _clipboardEntrys = new ObservableCollection<StringObject>();
        public ObservableCollection<StringObject> clipboardEntrys
        {
            get
            {
                if(currentSearch != "")
                {
                    return new ObservableCollection<StringObject>(_clipboardEntrys.Where(x => x.value.ToLower().Contains(currentSearch.ToLower())));
                }
                else
                    return _clipboardEntrys;
            }
            set
            {
                if (_clipboardEntrys != value)
                {
                    _clipboardEntrys = value;
                    RaisePropertyChanged("clipboardEntrys");
                }
            }
        }

        int _maxHistoryCount = 50;
        public int maxHistoryCount
        {
            get
            {
                return _maxHistoryCount;
            }
            set
            {
                if (_maxHistoryCount != value)
                {
                    _maxHistoryCount = value;
                    RaisePropertyChanged("maxHistoryCount");
                }
            }
        }

        public String currentVersion {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}

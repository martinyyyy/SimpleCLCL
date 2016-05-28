using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SimpleCLCL
{
    public class VM : INotifyPropertyChanged
    {
        private string _currentSearch = "";

        public string CurrentSearch
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
                    RaisePropertyChanged();
                    // ReSharper disable once ExplicitCallerInfoArgument
                    RaisePropertyChanged(nameof(ClipboardEntrys));
                    // ReSharper disable once ExplicitCallerInfoArgument
                    RaisePropertyChanged(nameof(IsSearchVisible));
                }
            }
        }

        private string _currentSelectedText;

        public string CurrentSelectedText
        {
            get
            {
                return _currentSelectedText;
            }
            set
            {
                if (_currentSelectedText != value)
                {
                    _currentSelectedText = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsSearchVisible => CurrentSearch != "";

        private bool _isPinningActive;

        public bool IsPinningActive
        {
            get
            {
                return _isPinningActive;
            }
            set
            {
                if (_isPinningActive != value)
                {
                    _isPinningActive = value;
                    RaisePropertyChanged();
                }
            }
        }

        private ObservableCollection<StringObject> _clipboardEntrys = new ObservableCollection<StringObject>();

        public ObservableCollection<StringObject> ClipboardEntrys
        {
            get
            {
                if (CurrentSearch != "")
                {
                    return new ObservableCollection<StringObject>(_clipboardEntrys.Where(x => x.Value.ToLower().Contains(CurrentSearch.ToLower())));
                }
                else
                {
                    return _clipboardEntrys;
                }
            }
            set
            {
                if (_clipboardEntrys != value)
                {
                    _clipboardEntrys = value;
                    RaisePropertyChanged();
                }
            }
        }

        private ObservableCollection<StringObject> _pinnedClipboardEntrys = new ObservableCollection<StringObject>();

        public ObservableCollection<StringObject> PinnedClipboardEntrys
        {
            get
            {
                if (CurrentSearch != "")
                {
                    return new ObservableCollection<StringObject>(_pinnedClipboardEntrys.Where(x => x.Value.ToLower().Contains(CurrentSearch.ToLower())));
                }
                else
                {
                    return _pinnedClipboardEntrys;
                }
            }
            set
            {
                if (_clipboardEntrys != value)
                {
                    _pinnedClipboardEntrys = value;
                    RaisePropertyChanged();
                }
            }
        }

        private int _maxHistoryCount = 50;

        public int MaxHistoryCount
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
                    RaisePropertyChanged();
                }
            }
        }

        public String CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
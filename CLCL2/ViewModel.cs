using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SimpleCLCL
{
    public class ViewModel : INotifyPropertyChanged
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

        bool _Startup;
        public bool Startup
        {
            get
            {
                return _Startup;
            }
            set
            {
                if (_Startup != value)
                {
                    _Startup = value;
                    RaisePropertyChanged("Startup");
                }
            }
        }


        public bool isWebUrl
        {
            get
            {
                Uri uriResult;
                bool result = Uri.TryCreate(CurrentSelectedText, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                return result;
            }
        }

        public bool isFilePath
        {
            get
            {
                String path = CurrentSelectedText;
                if(path == null)
                    return false;
                path = StripFilePath(path);
                
                return System.IO.Directory.Exists(path) || System.IO.File.Exists(path);
            }
        }

        public static String StripFilePath(String path)
        {
            path = path.Trim();
            if (path.StartsWith("\""))
                path = path.Remove(0, 1);
            if (path.EndsWith("\""))
                path = path.Remove(path.Length - 1, 1);
            return path;
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
                    RaisePropertyChanged(nameof(isWebUrl));
                    RaisePropertyChanged(nameof(isFilePath));
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
                    return new ObservableCollection<StringObject>(_clipboardEntrys.Where(x => x.Value.ToLower().Contains(RemoveNewLines(CurrentSearch.ToLower()))));
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

        private string _PlaceHolder = "<placeholder>";

        public string PlaceHolder
        {
            get
            {
                return _PlaceHolder;
            }
            set
            {
                if (_PlaceHolder != value)
                {
                    _PlaceHolder = value;
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

        public string CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal string RemoveNewLines(string currentSelectedText)
        {
            return currentSelectedText.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
        }

        #endregion INotifyPropertyChanged
    }
}
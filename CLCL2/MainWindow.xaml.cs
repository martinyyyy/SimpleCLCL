using Microsoft.Win32;
using NHotkey;
using NHotkey.Wpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CheckBox = System.Windows.Controls.CheckBox;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using ToolTip = System.Windows.Controls.ToolTip;

namespace SimpleCLCL
{
    public partial class MainWindow : Window
    {
        public ViewModel ViewModel => MainGrid.DataContext as ViewModel;

        private ToolTip _toolTip;
        private bool _forceTooltip;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                HotkeyManager.Current.AddOrReplace("OpenMenuSimpleCLCL", Key.C, ModifierKeys.Alt, OnMenuOpen);
            }
            catch (Exception)
            {
                MessageBox.Show("Something is blocking the ALT+C Hotkey. Maybe SimpleCLCL is already running? Closing SimpleCLCL now.");
                this.Close();
            }
            
            if (Properties.Settings.Default.historyItems <= 0)
            {
                Properties.Settings.Default.historyItems = 50;
            }
            
            ViewModel.MaxHistoryCount = Properties.Settings.Default.historyItems;
            Slider.ValueChanged += Slider_ValueChanged;

            if (Properties.Settings.Default.clipboardHistory != null)
            {
                foreach (var entry in Properties.Settings.Default.clipboardHistory)
                {
                    ViewModel.ClipboardEntrys.Add(new StringObject() { Value = entry });
                }
            }

            if (Properties.Settings.Default.pinnedClipboardHistory != null)
            {
                foreach (var entry in Properties.Settings.Default.pinnedClipboardHistory)
                {
                    ViewModel.PinnedClipboardEntrys.Add(new StringObject() { Value = entry, IsPinned = true });
                }
            }

            HideWindow();
            CheckStartup();
        }

        private void CheckStartup()
        {
            // THX @ http://stackoverflow.com/a/8695121
            // The path to the key where Windows looks for startup applications
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey(
                                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            ViewModel.Startup = rkApp.GetValue("SimpleCLCL") != null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ClipboardNotification.ClipboardUpdate += ClipboardNotification_ClipboardUpdate;           

            ViewModel.PropertyChanged += VM_PropertyChanged;
        }

        private void VM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.IsSearchVisible) && !ViewModel.IsSearchVisible)
            {
                FocusItem();
            }
        }

        private async void ClipboardNotification_ClipboardUpdate(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                bool done = false;
                for (int i = 0; i < 5 && !done; i++)
                {
                    // Delay if clipboard still open by other app
                    await Task.Delay(20);

                    try
                    {
                        var clipboradData = Clipboard.GetDataObject();

                        StringObject newObject = null;
                        if (clipboradData?.GetDataPresent(DataFormats.Text) ?? false)
                        {
                            newObject = new StringObject()
                            {
                                Value = clipboradData.GetData(DataFormats.Text).ToString()
                            };
                        }
                        
                        if (newObject != null)
                        {
                            if (newObject.Value.Trim().Length == 0)
                                return;

                            ViewModel.ClipboardEntrys.Remove(ViewModel.ClipboardEntrys.FirstOrDefault(x => x.Value == newObject.Value));
                            ViewModel.ClipboardEntrys.Insert(0, newObject);

                            if (ViewModel.ClipboardEntrys.Count > ViewModel.MaxHistoryCount)
                            {
                                ViewModel.ClipboardEntrys.Remove(ViewModel.ClipboardEntrys.Last());
                            }
                        }

                        done = true;
                    }
                    catch (COMException)
                    {
                        // Clipboard already opened
                    }
                }

                // Put text into clipboard from text popup
                if (InputTextPopup.IsOpen && done)
                {
                    PutInClipboard(true, false, ViewModel.ClipboardEntrys.First().Value);
                }
            }
        }

        private void OnMenuOpen(object sender, HotkeyEventArgs e)
        {
            if (this.IsVisible)
            {
                ToggleBetweenPinnedAndUnpinned();
            }
            else
            {
                ToggleBetweenPinnedAndUnpinned(true);
                ShowWindow();
            }

            e.Handled = true;
        }

        private void ToggleBetweenPinnedAndUnpinned(bool forceUnpinned = false)
        {
            if (forceUnpinned || CLipboardEntryListBox.ItemsSource == ViewModel.PinnedClipboardEntrys)
            {
                CLipboardEntryListBox.ItemsSource = ViewModel.ClipboardEntrys;
            }
            else
            {
                CLipboardEntryListBox.ItemsSource = ViewModel.PinnedClipboardEntrys;
            }

            ViewModel.IsPinningActive = CLipboardEntryListBox.ItemsSource == ViewModel.PinnedClipboardEntrys;

            FocusItem();
        }

        private void ShowWindow()
        {
            _forceTooltip = false;
            Point point = MouseCapture.GetMousePosition();

            // Multimonitor / DPI Fix

            var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
            point = transform.Transform(point);

            this.Left = point.X + 10;
            this.Top = point.Y - 10;

            var currScreen = Screen.PrimaryScreen;

            foreach (var screen in Screen.AllScreens)
            {
                // current screen
                if (screen.Bounds.IntersectsWith(new System.Drawing.Rectangle((int)this.Left, (int)this.Top, 1, 1)))
                {
                    currScreen = screen;
                }
            }

            if (this.Top + this.Height > currScreen.Bounds.Height)
                this.Top = currScreen.Bounds.Height - this.Height;

            this.Topmost = true;
            this.Show();
            this.Activate();

            CLipboardEntryListBox.SelectedIndex = 0;
            FocusItem();

            Storyboard sb = this.FindResource("ShowWindow") as Storyboard;

            sb.Completed += (sender, e) =>
            {
                CLipboardEntryListBox.KeyUp += CLipboardEntryListBoxKeyUp;
            };

            sb.Begin();
        }

        private void FocusItem()
        {
            CLipboardEntryListBox.UpdateLayout(); // Pre-generates item containers

            if (CLipboardEntryListBox.SelectedIndex == -1)
            {
                CLipboardEntryListBox.SelectedIndex = 0;
            }

            ListBoxItem listBoxItem = GetCurrentListboxItem();
            if (listBoxItem != null)
            {
                listBoxItem.Focus();
                CLipboardEntryListBox.ScrollIntoView(CLipboardEntryListBox.Items[0]);
            }
        }

        private ListBoxItem GetCurrentListboxItem()
        {
            if (CLipboardEntryListBox.SelectedIndex == -1)
            {
                CLipboardEntryListBox.SelectedIndex = 0;
            }

            return (ListBoxItem)CLipboardEntryListBox
                .ItemContainerGenerator
                .ContainerFromItem(CLipboardEntryListBox.SelectedItem);
        }

        private TChild FindVisualChild<TChild>(DependencyObject obj) where TChild : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is TChild)
                {
                    return (TChild)child;
                }

                var childOfChild = FindVisualChild<TChild>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            HideWindow();
        }

        private void HideWindow()
        {
            InputTextPopup.IsOpen = false;

            CLipboardEntryListBox.KeyUp -= CLipboardEntryListBoxKeyUp;
            if (ViewModel == null)
            {
                this.Hide();
                return;
            }

            ViewModel.CurrentSearch = "";

            Storyboard sb = this.FindResource("HideWindow") as Storyboard;
            sb.Begin();
            sb.Completed += (sender, e) =>
            {
                this.Hide();
                this.Topmost = false;
            };

            SaveSettings();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            if (ViewModel.ClipboardEntrys == null || ViewModel.PinnedClipboardEntrys == null) return;

            Properties.Settings.Default.clipboardHistory = new System.Collections.Specialized.StringCollection();
            foreach (StringObject entry in ViewModel.ClipboardEntrys)
            {
                Properties.Settings.Default.clipboardHistory.Add(entry.Value);
            }

            Properties.Settings.Default.pinnedClipboardHistory = new System.Collections.Specialized.StringCollection();
            foreach (StringObject entry in ViewModel.PinnedClipboardEntrys)
            {
                Properties.Settings.Default.pinnedClipboardHistory.Add(entry.Value);
            }

            Properties.Settings.Default.Save();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                HideWindow();
                e.Handled = true;
            }
        }

        private void CLipboardEntryListBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                _forceTooltip = true;
                OpenTooltip();
            }
            else if (e.Key == Key.Left)
            {
                _forceTooltip = false;
                if (_toolTip != null)
                    _toolTip.IsOpen = false;
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Space)
            {
                PutInClipboard();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                DeleteCurrentEntry();
            }
            else if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                PutInClipboard(false);
                e.Handled = true;
            }
            else if (e.Key == Key.LeftShift)
            {
                if (CLipboardEntryListBox.SelectedIndex + 1 >= CLipboardEntryListBox.Items.Count)
                {
                    CLipboardEntryListBox.SelectedIndex = 0;
                }
                else
                {
                    CLipboardEntryListBox.SelectedIndex++;
                }

                e.Handled = true;

                FocusItem();
            }
            else if (e.Key == Key.Tab)
            {
                if (CLipboardEntryListBox.SelectedIndex - 1 < 0)
                {
                    CLipboardEntryListBox.SelectedIndex = CLipboardEntryListBox.Items.Count - 1;
                }
                else
                {
                    CLipboardEntryListBox.SelectedIndex--;
                }

                e.Handled = true;

                FocusItem();
            }
            else if (!SearchBox.IsVisible && Keyboard.Modifiers.HasFlag(ModifierKeys.None) && e.Key.ToString().Length == 1 && e.Key != Key.C) // is a char on keyboard and we ignore C
            {
                ViewModel.CurrentSearch = e.Key.ToString();
                SearchBox.Focus();
                SearchBox.CaretIndex = 1;
            }
        }

        private void DeleteCurrentEntry()
        {
            if (CLipboardEntryListBox.SelectedIndex != -1)
            {
                ViewModel.ClipboardEntrys.RemoveAt(CLipboardEntryListBox.SelectedIndex);
            }
        }

        private async void PutInClipboard(bool insert = true, bool fromListbox = true, string text = "")
        {
            if (fromListbox && CLipboardEntryListBox.SelectedIndex == -1)
                return;

            if (fromListbox)
            {
                text = ViewModel.ClipboardEntrys[CLipboardEntryListBox.SelectedIndex].Value;
            }

            HideWindow();

            Clipboard.SetDataObject(text);

            if (insert)
            {
                await Task.Delay(250);
                SendKeys.SendWait("^v");
            }
        }

        private void ListBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
            {
                PutInClipboard();
            }
            else
            {
                OpenInputText();
            }
        }

        private void OpenInputText()
        {
            if (CLipboardEntryListBox.SelectedIndex == -1)
                return;

            InputTextPopup.IsOpen = true;
            ViewModel.CurrentSelectedText = ViewModel.ClipboardEntrys[CLipboardEntryListBox.SelectedIndex].Value;
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Enter)
            {
                FocusItem();
            }
            // TWO WAY BINDING KILLS EVERYTHING -> Ugly but works this way
            else if (ViewModel.IsPinningActive)
            {
                CLipboardEntryListBox.ItemsSource = ViewModel.PinnedClipboardEntrys;
            }
            else
            {
                CLipboardEntryListBox.ItemsSource = ViewModel.ClipboardEntrys;
            }
        }

        private void InitTooltip()
        {
            if (_toolTip == null)
            {
                _toolTip = new ToolTip();
                _toolTip.StaysOpen = false;
                _toolTip.IsOpen = true;
                _toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_forceTooltip)
            {
                OpenTooltip();
            }
        }

        private void OpenTooltip()
        {
            ListBoxItem listBoxItem = GetCurrentListboxItem();
            if (listBoxItem != null)
            {
                InitTooltip();
                _toolTip.IsOpen = true;
                _toolTip.Content = ((StringObject)CLipboardEntryListBox.SelectedItem).Value;
                _toolTip.PlacementTarget = listBoxItem;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.clipboardHistory = new System.Collections.Specialized.StringCollection();
            Properties.Settings.Default.Save();

            ViewModel.ClipboardEntrys.Clear();
        }

        private void PinChbx_Checked(object sender, RoutedEventArgs e)
        {
            var stringObject = (sender as CheckBox).DataContext as StringObject;
            ViewModel.ClipboardEntrys.Remove(stringObject);
            ViewModel.PinnedClipboardEntrys.Insert(0, stringObject);
        }

        private void PinChbx_Unchecked(object sender, RoutedEventArgs e)
        {
            var stringObject = (sender as CheckBox).DataContext as StringObject;
            ViewModel.ClipboardEntrys.Insert(0, stringObject);
            ViewModel.PinnedClipboardEntrys.Remove(stringObject);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ViewModel != null)
            {
                Properties.Settings.Default.historyItems = ViewModel.MaxHistoryCount;
                Properties.Settings.Default.Save();
            }
        }

        private void StartupChk_Checked(object sender, RoutedEventArgs e)
        {
            setAutostart(true);
        }

        private void StartupChk_Unchecked(object sender, RoutedEventArgs e)
        {
            setAutostart(false);
        }

        private void setAutostart(bool set)
        {
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey(
                               @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            string startPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs)
                               + @"\SimpleCLCL\SimpleCLCL.appref-ms";
            if(set)
                rkApp.SetValue("SimpleCLCL", startPath);
            else
                rkApp.DeleteValue("SimpleCLCL");

        }

        private void CandInsertBtn_Click(object sender, RoutedEventArgs e)
        {
            PutInClipboard(true, false, ViewModel.CurrentSelectedText);
        }

        private void RmvNewLineBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CurrentSelectedText = ViewModel.CurrentSelectedText.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
        }

        private void TrimBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CurrentSelectedText = ViewModel.CurrentSelectedText.Trim();
        }

        private void MaxOneSpaceBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CurrentSelectedText = Regex.Replace(ViewModel.CurrentSelectedText, "  +", " ");
        }

        private void BrowserButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ViewModel.CurrentSelectedText);
        }

        private void ExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            String path = ViewModel.StripFilePath(ViewModel.CurrentSelectedText);

            // Is not a folder, open folder an select file
            if (!System.IO.Directory.Exists(path))
                path = "/select," + path;

                Process.Start("explorer.exe", path);
        }
    }
}
using NHotkey;
using NHotkey.Wpf;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
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
        public event EventHandler<HotkeyEventArgs> HotKeyPressed;

        public VM VM => MainGrid.DataContext as VM;

        private ToolTip toolTip;
        private bool _forceTooltip = false;

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
            HideWindow();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ClipboardNotification.ClipboardUpdate += ClipboardNotification_ClipboardUpdate;

            if (Properties.Settings.Default.historyItems <= 0)
            {
                Properties.Settings.Default.historyItems = 50;
            }

            VM.MaxHistoryCount = Properties.Settings.Default.historyItems;

            if (Properties.Settings.Default.clipboardHistory != null)
            {
                foreach (var entry in Properties.Settings.Default.clipboardHistory)
                {
                    VM.ClipboardEntrys.Add(new StringObject() { Value = entry });
                }
            }

            if (Properties.Settings.Default.pinnedClipboardHistory != null)
            {
                foreach (var entry in Properties.Settings.Default.pinnedClipboardHistory)
                {
                    VM.PinnedClipboardEntrys.Add(new StringObject() { Value = entry, IsPinned = true });
                }
            }

            VM.PropertyChanged += VM_PropertyChanged;
        }

        private void VM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VM.IsSearchVisible) && !VM.IsSearchVisible)
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

                            VM.ClipboardEntrys.Remove(VM.ClipboardEntrys.FirstOrDefault(x => x.Value == newObject.Value));
                            VM.ClipboardEntrys.Insert(0, newObject);

                            if (VM.ClipboardEntrys.Count > VM.MaxHistoryCount)
                            {
                                VM.ClipboardEntrys.Remove(VM.ClipboardEntrys.Last());
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
                    PutInClipboard(true, false, VM.ClipboardEntrys.First().Value);
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
            if (forceUnpinned || CLipboardEntryListBox.ItemsSource == VM.PinnedClipboardEntrys)
            {
                CLipboardEntryListBox.ItemsSource = VM.ClipboardEntrys;
            }
            else
            {
                CLipboardEntryListBox.ItemsSource = VM.PinnedClipboardEntrys;
            }

            VM.IsPinningActive = CLipboardEntryListBox.ItemsSource == VM.PinnedClipboardEntrys;

            FocusItem();
        }

        private void ShowWindow()
        {
            _forceTooltip = false;
            Point point = MouseCapture.GetMousePosition();
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
            if (VM == null)
            {
                this.Hide();
                return;
            }

            VM.CurrentSearch = "";

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
            if (VM.ClipboardEntrys == null || VM.PinnedClipboardEntrys == null) return;

            Properties.Settings.Default.clipboardHistory = new System.Collections.Specialized.StringCollection();
            foreach (StringObject entry in VM.ClipboardEntrys)
            {
                Properties.Settings.Default.clipboardHistory.Add(entry.Value);
            }

            Properties.Settings.Default.pinnedClipboardHistory = new System.Collections.Specialized.StringCollection();
            foreach (StringObject entry in VM.PinnedClipboardEntrys)
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
                if (toolTip != null)
                    toolTip.IsOpen = false;
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
                VM.CurrentSearch = e.Key.ToString();
                SearchBox.Focus();
                SearchBox.CaretIndex = 1;
            }
        }

        private void DeleteCurrentEntry()
        {
            if (CLipboardEntryListBox.SelectedIndex != -1)
            {
                VM.ClipboardEntrys.RemoveAt(CLipboardEntryListBox.SelectedIndex);
            }
        }

        private async void PutInClipboard(bool insert = true, bool fromListbox = true, String text = "")
        {
            if (fromListbox && CLipboardEntryListBox.SelectedIndex == -1) return;

            HideWindow();

            if (fromListbox)
            {
                text = VM.ClipboardEntrys[CLipboardEntryListBox.SelectedIndex].Value;
            }

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
            if (CLipboardEntryListBox.SelectedIndex == -1) return;

            InputTextPopup.IsOpen = true;
            VM.CurrentSelectedText = VM.ClipboardEntrys[CLipboardEntryListBox.SelectedIndex].Value;
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Enter)
            {
                FocusItem();
            }
            // TWO WAY BINDING KILLS EVERYTHING -> Ugly but works this way
            else if (VM.IsPinningActive)
            {
                CLipboardEntryListBox.ItemsSource = VM.PinnedClipboardEntrys;
            }
            else
            {
                CLipboardEntryListBox.ItemsSource = VM.ClipboardEntrys;
            }
        }

        private void InitTooltip()
        {
            if (toolTip == null)
            {
                toolTip = new ToolTip();
                toolTip.StaysOpen = false;
                toolTip.IsOpen = true;
                toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_forceTooltip)
                OpenTooltip();
        }

        private void OpenTooltip()
        {
            ListBoxItem listBoxItem = GetCurrentListboxItem();
            if (listBoxItem != null)
            {
                InitTooltip();
                toolTip.IsOpen = true;
                toolTip.Content = ((StringObject)CLipboardEntryListBox.SelectedItem).Value;
                toolTip.PlacementTarget = listBoxItem;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.clipboardHistory = new System.Collections.Specialized.StringCollection();
            Properties.Settings.Default.Save();

            VM.ClipboardEntrys.Clear();
        }

        private void PinChbx_Checked(object sender, RoutedEventArgs e)
        {
            var stringObject = (sender as CheckBox).DataContext as StringObject;
            VM.ClipboardEntrys.Remove(stringObject);
            VM.PinnedClipboardEntrys.Insert(0, stringObject);
        }

        private void PinChbx_Unchecked(object sender, RoutedEventArgs e)
        {
            var stringObject = (sender as CheckBox).DataContext as StringObject;
            VM.ClipboardEntrys.Insert(0, stringObject);
            VM.PinnedClipboardEntrys.Remove(stringObject);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VM != null)
            {
                Properties.Settings.Default.historyItems = VM.MaxHistoryCount;
                Properties.Settings.Default.Save();
            }
        }
    }
}
﻿using NHotkey;
using NHotkey.Wpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SimpleCLCL
{
    public partial class MainWindow : Window
    {
        public event EventHandler<HotkeyEventArgs> HotKeyPressed;

        public VM VM => grid.DataContext as VM;

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
            if (e.PropertyName == "searchVisible" && !VM.IsSearchVisible)
            {
                FocusItem();
            }
        }

        private async void ClipboardNotification_ClipboardUpdate(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                bool done = false;
                for (int i = 0; i < 5 && done != true; i++)
                {
                    // Delay if clipboard still open by other app
                    if (!done)
                        await Task.Delay(20);

                    try
                    {
                        string text = Clipboard.GetDataObject().GetData(DataFormats.UnicodeText).ToString();

                        // dont put empty stuff in the history
                        if (text.Trim().Length == 0)
                            return;

                        VM.ClipboardEntrys.Remove(VM.ClipboardEntrys.FirstOrDefault(x => x.Value == text));
                        VM.ClipboardEntrys.Insert(0, new StringObject() { Value = text });

                        if (VM.ClipboardEntrys.Count > VM.MaxHistoryCount)
                            VM.ClipboardEntrys.Remove(VM.ClipboardEntrys.Last());

                        done = true;
                    }
                    catch (System.Runtime.InteropServices.COMException)
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
            if (forceUnpinned || listBox.ItemsSource == VM.PinnedClipboardEntrys)
                listBox.ItemsSource = VM.ClipboardEntrys;
            else
                listBox.ItemsSource = VM.PinnedClipboardEntrys;

            VM.IsPinningActive = listBox.ItemsSource == VM.PinnedClipboardEntrys;

            FocusItem();
        }

        private void ShowWindow()
        {
            _forceTooltip = false;
            Point point = MouseCapture.GetMousePosition();
            this.Left = point.X + 10;
            this.Top = point.Y - 10;

            System.Windows.Forms.Screen currScreen = System.Windows.Forms.Screen.PrimaryScreen;

            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                // current screen
                if (screen.Bounds.IntersectsWith(new System.Drawing.Rectangle((int)this.Left, (int)this.Top, 1, 1)))
                    currScreen = screen;
            }

            if (this.Top + this.Height > currScreen.Bounds.Height)
                this.Top = currScreen.Bounds.Height - this.Height;

            this.Topmost = true;
            this.Show();
            this.Activate();

            listBox.SelectedIndex = 0;
            FocusItem();

            Storyboard sb = this.FindResource("showWindow") as Storyboard;

            sb.Completed += (sender, e) =>
            {
                listBox.KeyUp += ListBox_KeyUp;
            };

            sb.Begin();
        }

        private void FocusItem()
        {
            listBox.UpdateLayout(); // Pre-generates item containers

            if (listBox.SelectedIndex == -1)
            {
                listBox.SelectedIndex = 0;
            }

            ListBoxItem listBoxItem = GetCurrentListboxItem();
            if (listBoxItem != null)
            {
                listBoxItem.Focus();
                listBox.ScrollIntoView(listBox.Items[0]);
            }
        }

        private ListBoxItem GetCurrentListboxItem()
        {
            if (listBox.SelectedIndex == -1)
            {
                listBox.SelectedIndex = 0;
            }

            return (ListBoxItem)listBox
                .ItemContainerGenerator
                .ContainerFromItem(listBox.SelectedItem);
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

            listBox.KeyUp -= ListBox_KeyUp;
            if (VM == null)
            {
                this.Hide();
                return;
            }

            VM.CurrentSearch = "";

            Storyboard sb = this.FindResource("hideWindow") as Storyboard;
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

        private void ListBox_KeyUp(object sender, KeyEventArgs e)
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
                if (listBox.SelectedIndex + 1 >= listBox.Items.Count)
                    listBox.SelectedIndex = 0;
                else
                    listBox.SelectedIndex++;

                e.Handled = true;

                FocusItem();
            }
            else if (e.Key == Key.Tab)
            {
                if (listBox.SelectedIndex - 1 < 0)
                    listBox.SelectedIndex = listBox.Items.Count - 1;
                else
                    listBox.SelectedIndex--;

                e.Handled = true;

                FocusItem();
            }
            else if (!searchBox.IsVisible && Keyboard.Modifiers.HasFlag(ModifierKeys.None) && e.Key.ToString().Length == 1 && e.Key != Key.C) // is a char on keyboard and we ignore C
            {
                VM.CurrentSearch = e.Key.ToString();
                searchBox.Focus();
                searchBox.CaretIndex = 1;
            }
        }

        private void DeleteCurrentEntry()
        {
            if (listBox.SelectedIndex != -1)
            {
                (listBox.ItemsSource as ObservableCollection<StringObject>).RemoveAt(listBox.SelectedIndex);
            }
        }

        private async void PutInClipboard(bool insert = true, bool fromListbox = true, String text = "")
        {
            if (fromListbox && listBox.SelectedIndex == -1) return;

            HideWindow();

            if (fromListbox)
            {
                text = (listBox.ItemsSource as ObservableCollection<StringObject>)[listBox.SelectedIndex].Value;
            }

            Clipboard.SetDataObject(text);

            if (insert)
            {
                await Task.Delay(250);
                System.Windows.Forms.SendKeys.SendWait("^v");
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
            if (listBox.SelectedIndex == -1) return;

            InputTextPopup.IsOpen = true;
            VM.CurrentSelectedText = (listBox.ItemsSource as ObservableCollection<StringObject>)[listBox.SelectedIndex].Value;
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
                listBox.ItemsSource = VM.PinnedClipboardEntrys;
            }
            else
            {
                listBox.ItemsSource = VM.ClipboardEntrys;
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
                toolTip.Content = ((StringObject)listBox.SelectedItem).Value;
                toolTip.PlacementTarget = listBoxItem;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            SimpleCLCL.Properties.Settings.Default.clipboardHistory = new System.Collections.Specialized.StringCollection();
            SimpleCLCL.Properties.Settings.Default.Save();

            VM.ClipboardEntrys.Clear();
        }

        private void PinChbx_Checked(object sender, RoutedEventArgs e)
        {
            StringObject s = (sender as CheckBox).DataContext as StringObject;
            VM.ClipboardEntrys.Remove(s);
            VM.PinnedClipboardEntrys.Insert(0, s);
        }

        private void PinChbx_Unchecked(object sender, RoutedEventArgs e)
        {
            StringObject s = (sender as CheckBox).DataContext as StringObject;
            VM.ClipboardEntrys.Insert(0, s);
            VM.PinnedClipboardEntrys.Remove(s);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VM != null)
            {
                SimpleCLCL.Properties.Settings.Default.historyItems = VM.MaxHistoryCount;
                SimpleCLCL.Properties.Settings.Default.Save();
            }
        }
    }
}
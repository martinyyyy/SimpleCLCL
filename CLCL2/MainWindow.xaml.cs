using NHotkey;
using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleCLCL
{
    public partial class MainWindow : Window
    {
        public event EventHandler<HotkeyEventArgs> HotKeyPressed;
        public VM VM { get; set; }

        ToolTip toolTip;
        bool forceTooltip = false;

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

            ClipboardNotification.ClipboardUpdate += ClipboardNotification_ClipboardUpdate;

            VM = new VM();
            DataContext = VM;

            if (SimpleCLCL.Properties.Settings.Default.clipboardHistory != null)
            {
                foreach (String entry in SimpleCLCL.Properties.Settings.Default.clipboardHistory)
                    VM.clipboardEntrys.Add(new StringObject() { value = entry });
            }

            if (SimpleCLCL.Properties.Settings.Default.pinnedClipboardHistory != null)
            {
                foreach (String entry in SimpleCLCL.Properties.Settings.Default.pinnedClipboardHistory)
                    VM.pinnedClipboardEntrys.Add(new StringObject() { value = entry, pinned=true });
            }

            hideWindow();

            VM.PropertyChanged += VM_PropertyChanged;
        }

        private void VM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "searchVisible" && !VM.searchVisible)
            {
                focusItem();
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
                        String text = Clipboard.GetDataObject().GetData(DataFormats.UnicodeText).ToString();

                        // dont put empty stuff in the history
                        if (text.Trim().Count() == 0)
                            return;

                        VM.clipboardEntrys.Remove(VM.clipboardEntrys.Where(x => x.value == text).FirstOrDefault());
                        VM.clipboardEntrys.Insert(0, new StringObject() { value = text });

                        if (VM.clipboardEntrys.Count > VM.maxHistoryCount)
                            VM.clipboardEntrys.Remove(VM.clipboardEntrys.Last());

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
                    putInClipboard(true, false, VM.clipboardEntrys.First().value);
                }
            }
        }

        private void OnMenuOpen(object sender, HotkeyEventArgs e)
        {
            if (this.IsVisible)
                toggleBetweenPinnedAndUnpinned();
            else
            {
                toggleBetweenPinnedAndUnpinned(true);
                showWindow();
            }

            e.Handled = true;
        }

        private void toggleBetweenPinnedAndUnpinned(bool forceUnpinned = false)
        {
            if (forceUnpinned || listBox.ItemsSource == VM.pinnedClipboardEntrys)
                listBox.ItemsSource = VM.clipboardEntrys;
            else
                listBox.ItemsSource = VM.pinnedClipboardEntrys;

            VM.pinnedActive = listBox.ItemsSource == VM.pinnedClipboardEntrys;

            focusItem();
        }

        private void showWindow()
        {
            forceTooltip = false;
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
            focusItem();

            Storyboard sb = this.FindResource("showWindow") as Storyboard;

            sb.Completed += (sender, e) =>
            {
                listBox.KeyUp += listBox_KeyUp;
            };

            sb.Begin();
        }

        private void focusItem()
        {
            listBox.UpdateLayout(); // Pre-generates item containers 

            if (listBox.SelectedIndex == -1)
                listBox.SelectedIndex = 0;

            ListBoxItem listBoxItem = getCurrentListboxItem();
            if (listBoxItem != null)
            {
                listBoxItem.Focus();
                listBox.ScrollIntoView(listBox.Items[0]);
            }
        }

        private ListBoxItem getCurrentListboxItem()
        {
            if (listBox.SelectedIndex == -1)
                listBox.SelectedIndex = 0;

            return (ListBoxItem)listBox
                .ItemContainerGenerator
                .ContainerFromItem(listBox.SelectedItem);
        }



        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            hideWindow();
        }

        private void hideWindow()
        {
            InputTextPopup.IsOpen = false;

            listBox.KeyUp -= listBox_KeyUp;
            if (VM == null)
            {
                this.Hide();
                return;
            }

            VM.currentSearch = "";

            Storyboard sb = this.FindResource("hideWindow") as Storyboard;
            sb.Begin();
            sb.Completed += (sender, e) =>
            {
                this.Hide();
                this.Topmost = false;
            };

            saveSettings();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            saveSettings();
        }

        private void saveSettings()
        {
            if (VM.clipboardEntrys == null || VM.pinnedClipboardEntrys == null) return;

            SimpleCLCL.Properties.Settings.Default.clipboardHistory = new System.Collections.Specialized.StringCollection();
            foreach (StringObject entry in VM.clipboardEntrys)
                SimpleCLCL.Properties.Settings.Default.clipboardHistory.Add(entry.value);

            SimpleCLCL.Properties.Settings.Default.pinnedClipboardHistory = new System.Collections.Specialized.StringCollection();
            foreach (StringObject entry in VM.pinnedClipboardEntrys)
                SimpleCLCL.Properties.Settings.Default.pinnedClipboardHistory.Add(entry.value);

            SimpleCLCL.Properties.Settings.Default.Save();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                hideWindow();
                e.Handled = true;
            }
        }

        private void listBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                forceTooltip = true;
                openTooltip();
            }
            else if (e.Key == Key.Left)
            {
                forceTooltip = false;
                if (toolTip != null)
                    toolTip.IsOpen = false;
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Space)
            {
                putInClipboard();
                e.Handled = true;
            }
            else if(e.Key == Key.Delete)
            {
                deleteCurrentEntry();
            }
            else if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                putInClipboard(false);
                e.Handled = true;
            }

            else if (e.Key == Key.LeftShift)
            {
                if (listBox.SelectedIndex + 1 >= listBox.Items.Count)
                    listBox.SelectedIndex = 0;
                else
                    listBox.SelectedIndex++;

                e.Handled = true;

                focusItem();
            }
            else if (e.Key == Key.Tab)
            {
                if (listBox.SelectedIndex - 1 < 0)
                    listBox.SelectedIndex = listBox.Items.Count - 1;
                else
                    listBox.SelectedIndex--;

                e.Handled = true;

                focusItem();
            }
            else if (!searchBox.IsVisible && Keyboard.Modifiers.HasFlag(ModifierKeys.None) && e.Key.ToString().Length == 1 && e.Key != Key.C) // is a char on keyboard and we ignore C
            {
                VM.currentSearch = e.Key.ToString();
                searchBox.Focus();
                searchBox.CaretIndex = 1;
            }
        }

        private void deleteCurrentEntry()
        {
            if(listBox.SelectedIndex != -1)
            (listBox.ItemsSource as ObservableCollection<StringObject>).RemoveAt(listBox.SelectedIndex);
        }

        private async void putInClipboard(bool insert = true, bool fromListbox = true, String text = "")
        {
            if (fromListbox && listBox.SelectedIndex == -1) return;

            hideWindow();

            if (fromListbox)
                text = (listBox.ItemsSource as ObservableCollection<StringObject>)[listBox.SelectedIndex].value;

            Clipboard.SetDataObject(text);

            if (insert)
            {
                await Task.Delay(250);
                System.Windows.Forms.SendKeys.SendWait("^v");
            }
        }

        private void listBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                putInClipboard();
            else openInputText();
        }

        private void openInputText()
        {
            if (listBox.SelectedIndex == -1) return;
            InputTextPopup.IsOpen = true;
            VM.currentSelectedText = (listBox.ItemsSource as ObservableCollection<StringObject>)[listBox.SelectedIndex].value;
        }

        private void searchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Enter)
                focusItem();
            // TWO WAY BINDING KILLS EVERYTHING -> Ugly but works this way
            else if (VM.pinnedActive)
                listBox.ItemsSource = VM.pinnedClipboardEntrys;
            else listBox.ItemsSource = VM.clipboardEntrys;
        }

        private void initTooltip()
        {
            if(toolTip == null)
            {
                toolTip = new ToolTip();
                toolTip.StaysOpen = false;
                toolTip.IsOpen = true;
                toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
            }
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(forceTooltip)
                openTooltip();
        }

        private void openTooltip()
        {
            ListBoxItem listBoxItem = getCurrentListboxItem();
            if (listBoxItem != null)
            {
                initTooltip();
                toolTip.IsOpen = true;
                toolTip.Content = ((StringObject)listBox.SelectedItem).value;
                toolTip.PlacementTarget = listBoxItem;
            }
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            SimpleCLCL.Properties.Settings.Default.clipboardHistory = new System.Collections.Specialized.StringCollection();
            SimpleCLCL.Properties.Settings.Default.Save();

            VM.clipboardEntrys.Clear();
        }

        private void pinChbx_Checked(object sender, RoutedEventArgs e)
        {
            StringObject s = (sender as CheckBox).DataContext as StringObject;
            VM.clipboardEntrys.Remove(s);
            VM.pinnedClipboardEntrys.Insert(0, s);
        }

        private void pinChbx_Unchecked(object sender, RoutedEventArgs e)
        {
            StringObject s = (sender as CheckBox).DataContext as StringObject;
            VM.clipboardEntrys.Insert(0, s);
            VM.pinnedClipboardEntrys.Remove(s);
        }

    }
}

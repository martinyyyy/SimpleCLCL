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

        public MainWindow()
        {
            try
            {
                HotkeyManager.Current.AddOrReplace("OpenMenuSimpleCLCL", Key.C, ModifierKeys.Alt, OnMenuOpen);
            }
            catch(Exception)
            {
                MessageBox.Show("Something is blocking the ALT+C Hotkey. Maybe SimpleCLCL is already running? Closing SimpleCLCL now.");
                this.Close();
            }

            ClipboardNotification.ClipboardUpdate += ClipboardNotification_ClipboardUpdate;

            VM = new VM();

            if (SimpleCLCL.Properties.Settings.Default.clipboardHistory != null)
            {
                foreach (String entry in SimpleCLCL.Properties.Settings.Default.clipboardHistory)
                    VM.clipboardEntrys.Add(new StringObject() { value = entry });
            }

            InitializeComponent();
            DataContext = VM;

            hideWindow();

        }

        private void ClipboardNotification_ClipboardUpdate(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                String text = Clipboard.GetText();
                VM.clipboardEntrys.Remove(VM.clipboardEntrys.Where(x => x.value == text).FirstOrDefault());
                VM.clipboardEntrys.Insert(0, new StringObject() { value = text });

                if (VM.clipboardEntrys.Count > VM.maxHistoryCount)
                    VM.clipboardEntrys.Remove(VM.clipboardEntrys.Last());
            }
        }

        private void OnMenuOpen(object sender, HotkeyEventArgs e)
        {
            if (this.IsVisible)
                hideWindow();
            else
                showWindow();

            e.Handled = true;
        }

        private void showWindow()
        {
            Point point = MouseCapture.GetMousePosition();
            this.Left = point.X-10;
            this.Top = point.Y-10;

            if (this.Top + this.Height > System.Windows.SystemParameters.PrimaryScreenHeight)
                this.Top = System.Windows.SystemParameters.PrimaryScreenHeight - this.Height;

            this.Topmost = true;
            this.Show();
            this.Activate();

            listBox.SelectedIndex = 0;
            focusItem();

            Storyboard sb = this.FindResource("showWindow") as Storyboard;
            sb.Begin();
        }

        private void focusItem()
        {
            listBox.UpdateLayout(); // Pre-generates item containers 

            var listBoxItem = (ListBoxItem)listBox
                .ItemContainerGenerator
                .ContainerFromItem(listBox.SelectedItem);

            if(listBoxItem != null)listBoxItem.Focus();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            hideWindow();
        }

        private void hideWindow()
        {
            Storyboard sb = this.FindResource("hideWindow") as Storyboard;
            sb.Begin();
            sb.Completed += (sender,e) =>
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
            SimpleCLCL.Properties.Settings.Default.clipboardHistory = new System.Collections.Specialized.StringCollection();
            foreach (StringObject entry in VM.clipboardEntrys)
                SimpleCLCL.Properties.Settings.Default.clipboardHistory.Add(entry.value);

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
            if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Space)
            {
                putInClipboard();
                e.Handled = true;
            }

            if(e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                putInClipboard(false);
                e.Handled = true;
            }


            if (e.Key == Key.S)
            {
                if (listBox.SelectedIndex + 1 >= listBox.Items.Count)
                    listBox.SelectedIndex = 0;
                else
                    listBox.SelectedIndex++;

                e.Handled = true;
            }
            else if(e.Key == Key.W)
            {
                if (listBox.SelectedIndex - 1 < 0)
                    listBox.SelectedIndex = listBox.Items.Count - 1;
                else
                    listBox.SelectedIndex--;

                e.Handled = true;
            }

            focusItem();
        }

        private async void putInClipboard(bool insert = true)
        {
            Clipboard.SetDataObject(VM.clipboardEntrys[listBox.SelectedIndex].value);
            hideWindow();

            if (insert)
            {
                await Task.Delay(250);
                System.Windows.Forms.SendKeys.SendWait("^v");
            }
        }

        private void listBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            putInClipboard();
        }
    }
}

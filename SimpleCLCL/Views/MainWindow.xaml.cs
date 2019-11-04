using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;
using SimpleCLCL.Utils;
using SimpleCLCL.ViewModel;
using Clipboard = System.Windows.Clipboard;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SimpleCLCL.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainViewModel MainViewModel => DataContext as MainViewModel;

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            RegisterHotkey();
        }

        private void RegisterHotkey()
        {
            new ClipboardManager(this).ClipboardTextChanged += MainViewModel.ClipboardTextChanged;
            try
            {
                HotkeyManager.Current.AddOrReplace("OpenMenuSimpleCLCL", Key.R, ModifierKeys.Alt, HotkeyPressed);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Something is blocking the ALT+C Hotkey. Maybe SimpleCLCL is already running? Closing SimpleCLCL now.");
                Close();
            }
        }

        public new void Show()
        {
            WindowHelper.SetPositionToMousePosition(this);
            base.Show();

            Activate();
            Topmost = true;
        }

        public new void Hide()
        {
            MainViewModel.Search = string.Empty;
            base.Hide();
            Topmost = false;
        }

        private void HotkeyPressed(object sender, HotkeyEventArgs e)
        {
            Show();
            e.Handled = true;
        }

        private void MainWindow_OnDeactivated(object sender, EventArgs e)
        {
            Hide();
        }

        private void MainWindow_OnActivated(object sender, EventArgs e)
        {
            FocusHelper.FocusFirstItem(ClipboardEntrysListbox);
        }
        
        private void ClipboardEntrysListbox_OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    InsertClipboard(MainViewModel.SelectedItem);
                    break;
                case Key.Escape:
                    Hide();
                    break;
                case Key.Up: case Key.Down: case Key.Left:case Key.Right:
                    break;
                default:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.None) && e.Key.ToString().Length == 1)
                    {
                        MainViewModel.Search = e.Key.ToString();
                        SearchBox.Focus();
                        SearchBox.CaretIndex = 1;
                    }

                    break;
            }
        }

        private void SearchBox_OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Hide();
                    break;
                case Key.Down:
                    FocusHelper.FocusFirstItem(ClipboardEntrysListbox);
                    break;
            }

        }

        public async void InsertClipboard(String text)
        {
            Clipboard.SetDataObject(text);
            Hide();

            await Task.Delay(250);
            SendKeys.SendWait("^v");
        }

        private void ClipboardEntrysListbox_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
                InsertClipboard(MainViewModel.SelectedItem);
            else if(e.ChangedButton == MouseButton.Right)
                MessageBox.Show("rechtsklick");
        }
    }
}
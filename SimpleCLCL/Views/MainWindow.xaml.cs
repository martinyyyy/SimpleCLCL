using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;
using SimpleCLCL.Utils;
using SimpleCLCL.ViewModel;
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
            SetPositionToMousePosition();
            base.Show();

            Activate();
            Topmost = true;
        }

        public new void Hide()
        {
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
            ClipboardEntrysListbox.SelectedIndex = 0;
            FocusHelper.FocusFirstItem(ClipboardEntrysListbox);
        }

        private void SetPositionToMousePosition()
        {
            var point = MouseCapture.GetMousePosition();

            // Multimonitor / DPI Fix
            var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
            point = transform.Transform(point);

            Left = point.X + 10;
            Top = point.Y - 10;

            var currScreen = Screen.PrimaryScreen;
            foreach (var screen in Screen.AllScreens)
                if (screen.Bounds.IntersectsWith(new Rectangle((int)Left, (int)Top, 1, 1)))
                    currScreen = screen;

            if (Top + Height > currScreen.Bounds.Height)
                Top = currScreen.Bounds.Height - Height;
        }
    }
}
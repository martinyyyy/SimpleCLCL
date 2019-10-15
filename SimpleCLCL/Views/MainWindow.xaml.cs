using System.Windows;
using SimpleCLCL.Utils;
using SimpleCLCL.ViewModel;

namespace SimpleCLCL.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel MainViewModel => this.DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            new ClipboardManager(this).ClipboardTextChanged += MainViewModel.ClipboardTextChanged;
        }
    }
}

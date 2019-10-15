using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace SimpleCLCL.Utils
{
    public class ClipboardManager
    {
        public event EventHandler<ClipboardTextChangedEventArgs> ClipboardTextChanged;

        public ClipboardManager(Window windowSource)
        {
            HwndSource source = PresentationSource.FromVisual(windowSource) as HwndSource;
            if (source == null)
            {
                throw new ArgumentException(
                    "Window source MUST be initialized first, such as in the Window's OnSourceInitialized handler."
                    , nameof(windowSource));
            }

            source.AddHook(WndProc);

            // get window handle for interop
            IntPtr windowHandle = new WindowInteropHelper(windowSource).Handle;

            // register for clipboard events
            NativeMethods.AddClipboardFormatListener(windowHandle);
        }

        private async void OnClipboardTextChanged()
        {
            if (Clipboard.ContainsText())
            {
                // Delay if clipboard still open by other app
                await Task.Delay(10);
                string ret = string.Empty;

                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        var clipboardData = Clipboard.GetDataObject();

                        if (clipboardData?.GetDataPresent(DataFormats.Text) ?? false)
                        {
                            ret = clipboardData.GetData(DataFormats.Text)?.ToString();
                            break;
                        }
                    }
                    catch (COMException)
                    {
                        // Clipboard already opened
                        // Delay if clipboard still open by other app
                        await Task.Delay(10);
                    }
                }

                if(!string.IsNullOrEmpty(ret)) 
                    ClipboardTextChanged?.Invoke(this, new ClipboardTextChangedEventArgs(ret));
            }
        }

        private static readonly IntPtr WndProcSuccess = IntPtr.Zero;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                OnClipboardTextChanged();
                handled = true;
            }

            return WndProcSuccess;
        }
    }

    public class ClipboardTextChangedEventArgs : EventArgs
    {
        public string Text { get; }

        public ClipboardTextChangedEventArgs(string text)
        {
            Text = text;
        }
    }
}

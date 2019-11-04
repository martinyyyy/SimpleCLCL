using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace SimpleCLCL.Utils
{
    class WindowHelper
    {
        public static void SetPositionToMousePosition(Window window)
        {
            var point = MouseCapture.GetMousePosition();

            // Multimonitor / DPI Fix
            var transform = PresentationSource.FromVisual(window).CompositionTarget.TransformFromDevice;
            point = transform.Transform(point);

            window.Left = point.X + 10;
            window.Top = point.Y - 10;

            var currScreen = Screen.PrimaryScreen;
            foreach (var screen in Screen.AllScreens)
                if (screen.Bounds.IntersectsWith(new Rectangle((int)window.Left, (int)window.Top, 1, 1)))
                    currScreen = screen;

            if (window.Top + window.Height > currScreen.Bounds.Height)
                window.Top = currScreen.Bounds.Height - window.Height;
        }
    }
}

using System;
using System.Runtime.InteropServices;
using System.Windows;
using SimpleCLCL.Utils;

namespace SimpleCLCL.Utils
{
    internal class MouseCapture
    {
        public static Point GetMousePosition()
        {
            NativeMethods.Win32Point w32Mouse = new NativeMethods.Win32Point();
            NativeMethods.GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }
    }
}
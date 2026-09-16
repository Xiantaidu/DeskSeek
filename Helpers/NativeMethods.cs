using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace DeskSeek.Helpers
{
    public static class NativeMethods
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const uint GA_PARENT = 1;
        public const uint GA_ROOT = 2;
        public const uint GA_ROOTOWNER = 3;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        public static void SetNoActivate(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }

        public static bool TryGetWindowDipPosition(IntPtr hWnd, Visual visual, out double dipLeft, out double dipTop)
        {
            dipLeft = 0;
            dipTop = 0;
            if (hWnd == IntPtr.Zero || !GetWindowRect(hWnd, out RECT rect))
                return false;

            var dpi = VisualTreeHelper.GetDpi(visual);
            double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

            dipLeft = rect.Left / scaleX;
            dipTop = rect.Top / scaleY;
            return true;
        }
    }
}

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

namespace DeskSeek.Helpers
{
    public static class ScreenHelper
    {
        public struct ScreenWorkArea
        {
            public double Left;
            public double Top;
            public double Right;
            public double Bottom;
            public double Width;
            public double Height;
            public double DpiScaleX;
            public double DpiScaleY;
        }

        public static ScreenWorkArea GetWorkArea(Window window)
        {
            var helper = new WindowInteropHelper(window);
            var hWnd = helper.EnsureHandle();

            var screen = Screen.FromHandle(hWnd);
            var dpi = VisualTreeHelper.GetDpi(window);
            double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

            var wa = screen.WorkingArea;
            double left = wa.Left / scaleX;
            double top = wa.Top / scaleY;
            double width = wa.Width / scaleX;
            double height = wa.Height / scaleY;

            return new ScreenWorkArea
            {
                Left = left,
                Top = top,
                Right = left + width,
                Bottom = top + height,
                Width = width,
                Height = height,
                DpiScaleX = scaleX,
                DpiScaleY = scaleY
            };
        }

        public static ScreenWorkArea GetWorkAreaFromPoint(double dipX, double dipY, double fallbackScale = 1.0)
        {
            int physX = (int)(dipX * fallbackScale);
            int physY = (int)(dipY * fallbackScale);
            var screen = Screen.FromPoint(new System.Drawing.Point(physX, physY));

            return new ScreenWorkArea
            {
                Left = screen.WorkingArea.Left / fallbackScale,
                Top = screen.WorkingArea.Top / fallbackScale,
                Right = screen.WorkingArea.Right / fallbackScale,
                Bottom = screen.WorkingArea.Bottom / fallbackScale,
                Width = screen.WorkingArea.Width / fallbackScale,
                Height = screen.WorkingArea.Height / fallbackScale,
                DpiScaleX = fallbackScale,
                DpiScaleY = fallbackScale
            };
        }
    }
}

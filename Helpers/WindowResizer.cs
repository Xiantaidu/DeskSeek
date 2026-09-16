using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeskSeek.Services;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;

namespace DeskSeek.Helpers
{
    public class WindowResizer
    {
        private readonly Window _window;
        private readonly SettingsService _settingsService;
        private readonly FrameworkElement _leftGrip;
        private readonly FrameworkElement _rightGrip;
        private readonly FrameworkElement _bottomGrip;

        private bool _isResizingWidth;
        private bool _isResizingHeight;
        private bool _isResizingFromLeft;
        private Point _resizeStartMouseScreen;
        private double _resizeStartWidth;
        private double _resizeStartHeight;
        private double _resizeStartLeft;

        public bool IsResizing => _isResizingWidth || _isResizingHeight;
        public event Action? ResizeCompleted;

        public WindowResizer(
            Window window,
            SettingsService settingsService,
            FrameworkElement leftGrip,
            FrameworkElement rightGrip,
            FrameworkElement bottomGrip)
        {
            _window = window;
            _settingsService = settingsService;
            _leftGrip = leftGrip;
            _rightGrip = rightGrip;
            _bottomGrip = bottomGrip;

            AttachEvents();
        }

        private void AttachEvents()
        {
            _leftGrip.MouseEnter += ResizeGrip_MouseEnter;
            _leftGrip.MouseLeave += ResizeGrip_MouseLeave;
            _leftGrip.MouseLeftButtonDown += (s, e) => StartWidthResize(e, isFromLeft: true);
            _leftGrip.MouseMove += ResizeGrip_MouseMove;
            _leftGrip.MouseLeftButtonUp += ResizeGrip_MouseLeftButtonUp;

            _rightGrip.MouseEnter += ResizeGrip_MouseEnter;
            _rightGrip.MouseLeave += ResizeGrip_MouseLeave;
            _rightGrip.MouseLeftButtonDown += (s, e) => StartWidthResize(e, isFromLeft: false);
            _rightGrip.MouseMove += ResizeGrip_MouseMove;
            _rightGrip.MouseLeftButtonUp += ResizeGrip_MouseLeftButtonUp;

            _bottomGrip.MouseEnter += BottomResizeGrip_MouseEnter;
            _bottomGrip.MouseLeave += BottomResizeGrip_MouseLeave;
            _bottomGrip.MouseLeftButtonDown += BottomResizeGrip_MouseLeftButtonDown;
            _bottomGrip.MouseMove += BottomResizeGrip_MouseMove;
            _bottomGrip.MouseLeftButtonUp += BottomResizeGrip_MouseLeftButtonUp;
        }

        private void ResizeGrip_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement grip)
            {
                if (grip.FindName("LeftResizeBar") is Border leftBar)
                {
                    leftBar.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x52, 0xD9));
                    leftBar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150)));
                }
                if (grip.FindName("RightResizeBar") is Border rightBar)
                {
                    rightBar.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x52, 0xD9));
                    rightBar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150)));
                }
            }
        }

        private void ResizeGrip_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isResizingWidth) return;
            if (sender is FrameworkElement grip)
            {
                if (grip.FindName("LeftResizeBar") is Border leftBar)
                {
                    leftBar.Background = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));
                    leftBar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.5, TimeSpan.FromMilliseconds(150)));
                }
                if (grip.FindName("RightResizeBar") is Border rightBar)
                {
                    rightBar.Background = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));
                    rightBar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.5, TimeSpan.FromMilliseconds(150)));
                }
            }
        }

        private void StartWidthResize(MouseButtonEventArgs e, bool isFromLeft)
        {
            var grip = isFromLeft ? _leftGrip : _rightGrip;
            _isResizingWidth = true;
            _isResizingFromLeft = isFromLeft;
            _resizeStartWidth = _window.Width;
            _resizeStartLeft = _window.Left;
            _resizeStartMouseScreen = grip.PointToScreen(e.GetPosition(grip));

            grip.CaptureMouse();
            e.Handled = true;
        }

        private void ResizeGrip_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizingWidth) return;

            var grip = sender as FrameworkElement ?? (_isResizingFromLeft ? _leftGrip : _rightGrip);
            Point currentScreen = grip.PointToScreen(e.GetPosition(grip));

            var dpi = VisualTreeHelper.GetDpi(_window);
            double deltaScreenX = currentScreen.X - _resizeStartMouseScreen.X;
            double deltaDipX = deltaScreenX / (dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0);

            var wa = ScreenHelper.GetWorkArea(_window);
            double minWidth = 360;
            double maxWidth = Math.Max(minWidth, wa.Width - 60);

            if (_isResizingFromLeft)
            {
                // Opposite Edge Pinning: fix the right edge
                double rightEdge = _resizeStartLeft + _resizeStartWidth;
                double proposedWidth = _resizeStartWidth - deltaDipX;
                double newWidth = Math.Clamp(proposedWidth, minWidth, maxWidth);

                _window.Width = newWidth;
                _window.Left = rightEdge - newWidth;
            }
            else
            {
                // Fix the left edge
                double proposedWidth = _resizeStartWidth + deltaDipX;
                double newWidth = Math.Clamp(proposedWidth, minWidth, maxWidth);

                _window.Width = newWidth;
                _window.Left = _resizeStartLeft;
            }
        }

        private void ResizeGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizingWidth)
            {
                _isResizingWidth = false;
                (sender as FrameworkElement)?.ReleaseMouseCapture();

                _settingsService.Current.DrawerWidth = _window.Width;
                if (_settingsService.Current.RememberX)
                {
                    _settingsService.Current.LastDrawerX = _window.Left;
                }
                _settingsService.Save();

                ResizeGrip_MouseLeave(sender, e);
                ResizeCompleted?.Invoke();
            }
        }

        private void BottomResizeGrip_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_bottomGrip.FindName("BottomResizeBar") is Border bottomBar)
            {
                bottomBar.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x52, 0xD9));
                bottomBar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150)));
            }
        }

        private void BottomResizeGrip_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isResizingHeight) return;
            if (_bottomGrip.FindName("BottomResizeBar") is Border bottomBar)
            {
                bottomBar.Background = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));
                bottomBar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.5, TimeSpan.FromMilliseconds(150)));
            }
        }

        private void BottomResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isResizingHeight = true;
            _resizeStartHeight = _window.Height;
            _resizeStartMouseScreen = _bottomGrip.PointToScreen(e.GetPosition(_bottomGrip));

            _bottomGrip.CaptureMouse();
            e.Handled = true;
        }

        private void BottomResizeGrip_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizingHeight) return;

            Point currentScreen = _bottomGrip.PointToScreen(e.GetPosition(_bottomGrip));
            var dpi = VisualTreeHelper.GetDpi(_window);
            double deltaScreenY = currentScreen.Y - _resizeStartMouseScreen.Y;
            double deltaDipY = deltaScreenY / (dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0);

            var wa = ScreenHelper.GetWorkArea(_window);
            double minHeight = 400;
            double maxHeight = Math.Max(minHeight, wa.Bottom - _window.Top);

            double proposedHeight = _resizeStartHeight + deltaDipY;
            double newHeight = Math.Clamp(proposedHeight, minHeight, maxHeight);

            _window.Height = newHeight;
        }

        private void BottomResizeGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizingHeight)
            {
                _isResizingHeight = false;
                _bottomGrip.ReleaseMouseCapture();

                _settingsService.Current.DrawerHeight = _window.Height;
                if (_settingsService.Current.RememberY)
                {
                    _settingsService.Current.LastDrawerY = _window.Top;
                }
                _settingsService.Save();

                BottomResizeGrip_MouseLeave(sender, e);
                ResizeCompleted?.Invoke();
            }
        }
    }
}

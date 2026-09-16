using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DeskSeek.Helpers;
using DeskSeek.Services;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace DeskSeek.Views
{
    public partial class FloatingBallWindow : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private readonly SettingsService _settingsService;
        private readonly DispatcherTimer _collapseTimer;

        private Point _mouseDownPos;
        private bool _isPotentialClick;
        private bool _isCollapsed;
        private bool _isDockedToRight = true;
        private bool _isAnimating;
        private DateTime _lastClickTime = DateTime.MinValue;

        public event Action? BallClicked;
        public bool IsDrawerOpen { get; set; }

        public bool IsDockedToRight => IsCurrentlyOnRightSide();

        public IntPtr WindowHandle { get; private set; }

        public FloatingBallWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;

            _collapseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2.5)
            };
            _collapseTimer.Tick += CollapseTimer_Tick;

            Loaded += FloatingBallWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Configure window as non-activating so clicking the ball does not steal focus
            var helper = new WindowInteropHelper(this);
            WindowHandle = helper.Handle;

            int exStyle = GetWindowLong(WindowHandle, GWL_EXSTYLE);
            SetWindowLong(WindowHandle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }

        private void FloatingBallWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var wa = ScreenHelper.GetWorkArea(this);

            _isDockedToRight = _settingsService.Current.IsDockedToRight;

            double initialTop = _settingsService.Current.BallTop;
            if (initialTop < wa.Top || initialTop > wa.Bottom - ActualHeight)
            {
                initialTop = wa.Top + 200;
            }

            Top = Math.Clamp(initialTop, wa.Top + 10, wa.Bottom - ActualHeight - 10);

            if (_isDockedToRight)
            {
                Left = wa.Right - ActualWidth;
            }
            else
            {
                Left = wa.Left;
            }

            _collapseTimer.Start();
        }

        public bool IsCurrentlyOnRightSide()
        {
            if (WindowHandle != IntPtr.Zero && GetWindowRect(WindowHandle, out RECT rect))
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                double dipLeft = rect.Left / scaleX;
                var wa = ScreenHelper.GetWorkArea(this);
                double ballCenter = dipLeft + (ActualWidth > 0 ? ActualWidth : 52) / 2;
                double screenCenter = wa.Left + wa.Width / 2;
                return ballCenter >= screenCenter;
            }
            return _isDockedToRight;
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            _collapseTimer.Stop();
            var story = (Storyboard)Resources["OnMouseEnterStory"];
            story.Begin();

            if (_isCollapsed)
            {
                ExpandFromEdge();
            }
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            var story = (Storyboard)Resources["OnMouseLeaveStory"];
            story.Begin();

            if (!IsDrawerOpen)
            {
                _collapseTimer.Stop();
                _collapseTimer.Start();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDownPos = e.GetPosition(this);
            _isPotentialClick = true;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _isPotentialClick)
            {
                Point current = e.GetPosition(this);
                if (Math.Abs(current.X - _mouseDownPos.X) > 6 || Math.Abs(current.Y - _mouseDownPos.Y) > 6)
                {
                    _isPotentialClick = false;
                    _collapseTimer.Stop();

                    if (_isCollapsed)
                    {
                        ExpandFromEdge(instant: true);
                    }

                    try
                    {
                        DragMove();
                    }
                    catch { }

                    OnDragCompleted();
                }
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPotentialClick)
            {
                _isPotentialClick = false;

                // Debounce rapid accidental double clicks
                if ((DateTime.Now - _lastClickTime).TotalMilliseconds < 320)
                    return;
                _lastClickTime = DateTime.Now;

                BallClicked?.Invoke();
            }
        }

        private void OnDragCompleted()
        {
            // Read true current window position from Win32 HWND after DragMove
            double dipLeft = Left;
            double dipTop = Top;

            if (WindowHandle != IntPtr.Zero && GetWindowRect(WindowHandle, out RECT rect))
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
                dipLeft = rect.Left / scaleX;
                dipTop = rect.Top / scaleY;

                Left = dipLeft;
                Top = dipTop;
            }

            var wa = ScreenHelper.GetWorkArea(this);

            double centerX = dipLeft + ActualWidth / 2;
            double screenCenterX = wa.Left + wa.Width / 2;

            _isDockedToRight = centerX >= screenCenterX;

            double targetLeft = _isDockedToRight ? (wa.Right - ActualWidth) : wa.Left;
            double targetTop = Math.Clamp(dipTop, wa.Top + 15, wa.Bottom - ActualHeight - 15);

            AnimatePosition(targetLeft, targetTop, () =>
            {
                _settingsService.Current.BallTop = targetTop;
                _settingsService.Current.BallLeft = targetLeft;
                _settingsService.Current.IsDockedToRight = _isDockedToRight;
                _settingsService.Save();

                if (!IsDrawerOpen && !IsMouseOver)
                {
                    _collapseTimer.Stop();
                    _collapseTimer.Start();
                }
            });
        }

        private void CollapseTimer_Tick(object? sender, EventArgs e)
        {
            _collapseTimer.Stop();

            if (!_settingsService.Current.AutoCollapse)
                return;

            if (IsMouseOver || IsDrawerOpen || _isAnimating)
                return;

            CollapseToEdge();
        }

        private void CollapseToEdge()
        {
            var wa = ScreenHelper.GetWorkArea(this);
            double visibleEdgeWidth = 16.0;

            _isDockedToRight = IsCurrentlyOnRightSide();

            double targetLeft = _isDockedToRight
                ? (wa.Right - visibleEdgeWidth)
                : (wa.Left - (ActualWidth - visibleEdgeWidth));

            _isAnimating = true;

            var animLeft = new DoubleAnimation(Left, targetLeft, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseInOut }
            };
            var animOpacity = new DoubleAnimation(Opacity, 0.60, TimeSpan.FromMilliseconds(320));

            animLeft.Completed += (s, ev) =>
            {
                _isCollapsed = true;
                _isAnimating = false;
                Left = targetLeft;
                Opacity = 0.60;
            };

            BeginAnimation(LeftProperty, animLeft);
            BeginAnimation(OpacityProperty, animOpacity);
        }

        public void ExpandFromEdge(bool instant = false)
        {
            _collapseTimer.Stop();
            if (!_isCollapsed && !instant)
                return; // Already expanded, do not trigger twitching animation

            var wa = ScreenHelper.GetWorkArea(this);
            _isDockedToRight = IsCurrentlyOnRightSide();
            double targetLeft = _isDockedToRight ? (wa.Right - ActualWidth) : wa.Left;

            if (instant)
            {
                BeginAnimation(LeftProperty, null);
                BeginAnimation(OpacityProperty, null);
                Left = targetLeft;
                Opacity = 1.0;
                _isCollapsed = false;
                return;
            }

            _isAnimating = true;
            var animLeft = new DoubleAnimation(Left, targetLeft, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut }
            };
            var animOpacity = new DoubleAnimation(Opacity, 1.0, TimeSpan.FromMilliseconds(240));

            animLeft.Completed += (s, ev) =>
            {
                _isCollapsed = false;
                _isAnimating = false;
                Left = targetLeft;
                Opacity = 1.0;
            };

            BeginAnimation(LeftProperty, animLeft);
            BeginAnimation(OpacityProperty, animOpacity);
        }

        private void AnimatePosition(double targetLeft, double targetTop, Action? onCompleted = null)
        {
            _isAnimating = true;

            var animLeft = new DoubleAnimation(Left, targetLeft, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            };
            var animTop = new DoubleAnimation(Top, targetTop, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            };

            animLeft.Completed += (s, e) =>
            {
                _isAnimating = false;
                Left = targetLeft;
                Top = targetTop;
                onCompleted?.Invoke();
            };

            BeginAnimation(LeftProperty, animLeft);
            BeginAnimation(TopProperty, animTop);
        }

        public void NotifyDrawerStateChanged(bool isOpen)
        {
            IsDrawerOpen = isOpen;
            if (isOpen)
            {
                _collapseTimer.Stop();
                ExpandFromEdge();
            }
            else
            {
                if (!IsMouseOver)
                {
                    _collapseTimer.Stop();
                    _collapseTimer.Start();
                }
            }
        }
    }
}

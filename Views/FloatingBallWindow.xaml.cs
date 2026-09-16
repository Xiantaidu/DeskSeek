using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DeskSeek.Helpers;
using DeskSeek.Models;
using DeskSeek.Services;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace DeskSeek.Views
{
    public partial class FloatingBallWindow : Window
    {

        private readonly SettingsService _settingsService;
        private readonly DispatcherTimer _collapseTimer;

        private Point _mouseDownPos;
        private bool _isPotentialClick;
        private bool _isCollapsed;
        private bool _isDockedToRight = true;
        private bool _isAnimating;
        private DateTime _lastClickTime = DateTime.MinValue;

        public event Action? BallClicked;
        public event Action? SettingsRequested;
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
            NativeMethods.SetNoActivate(WindowHandle);
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
            if (NativeMethods.TryGetWindowDipPosition(WindowHandle, this, out double dipLeft, out _))
            {
                var wa = ScreenHelper.GetWorkArea(this);
                double ballCenter = dipLeft + (ActualWidth > 0 ? ActualWidth : 96) / 2;
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

            if (!IsDrawerOpen && (BallContextMenu == null || !BallContextMenu.IsOpen))
            {
                _collapseTimer.Stop();
                _collapseTimer.Start();
            }
        }

        private void Window_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isPotentialClick = false;
            _collapseTimer.Stop();

            if (_isCollapsed)
            {
                ExpandFromEdge(instant: true);
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

            if (NativeMethods.TryGetWindowDipPosition(WindowHandle, this, out double nativeLeft, out double nativeTop))
            {
                dipLeft = nativeLeft;
                dipTop = nativeTop;
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
            // Expose exactly half of the icon/ball when collapsed
            double visibleEdgeWidth = (ActualWidth > 0 ? ActualWidth : 96.0) / 2.0;

            _isDockedToRight = IsCurrentlyOnRightSide();

            double targetLeft = _isDockedToRight
                ? (wa.Right - visibleEdgeWidth)
                : (wa.Left - (ActualWidth - visibleEdgeWidth));

            _isAnimating = true;

            var animLeft = new DoubleAnimation(Left, targetLeft, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseInOut }
            };
            var animOpacity = new DoubleAnimation(Opacity, 0.85, TimeSpan.FromMilliseconds(320));

            animLeft.Completed += (s, ev) =>
            {
                _isCollapsed = true;
                _isAnimating = false;
                Left = targetLeft;
                Opacity = 0.85;
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

        public void ResetBallPosition()
        {
            var wa = ScreenHelper.GetWorkArea(this);
            _isDockedToRight = true;
            double targetTop = wa.Top + 200;
            double targetLeft = wa.Right - ActualWidth;

            Top = targetTop;
            Left = targetLeft;

            if (_settingsService != null)
            {
                _settingsService.Current.BallTop = targetTop;
                _settingsService.Current.BallLeft = targetLeft;
                _settingsService.Current.IsDockedToRight = true;
                _settingsService.Save();
            }

            ExpandFromEdge(instant: true);
        }

        private void BallContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            _collapseTimer.Stop();

            string hk = _settingsService?.Current.Hotkey ?? "Alt+D";
            string toggleBase = LocalizationService.Instance.GetString("Lang_MenuToggle");
            if (string.IsNullOrWhiteSpace(hk) || hk == "无" || hk.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                MenuToggleItem.Header = toggleBase;
            }
            else
            {
                MenuToggleItem.Header = $"{toggleBase} ({hk})";
            }

            MenuAutoStartItem.Header = LocalizationService.Instance.GetString("Lang_MenuAutoStart");
            MenuPinTopmostItem.Header = LocalizationService.Instance.GetString("Lang_PinTopmostHeader");
            MenuPinNormalItem.Header = LocalizationService.Instance.GetString("Lang_PinNormalHeader");
            MenuPinAutoHideItem.Header = LocalizationService.Instance.GetString("Lang_PinAutoHideHeader");
            MenuSettingsItem.Header = LocalizationService.Instance.GetString("Lang_MenuSettings");
            MenuResetBallItem.Header = LocalizationService.Instance.GetString("Lang_MenuResetBall");
            MenuOpenBrowserItem.Header = LocalizationService.Instance.GetString("Lang_MenuOpenBrowser");
            MenuExitItem.Header = LocalizationService.Instance.GetString("Lang_MenuExit");

            MenuAutoStartItem.IsChecked = AutoStartService.IsAutoStartEnabled();
            var currentMode = _settingsService?.Current.PinMode ?? DrawerPinMode.AutoHide;
            MenuPinTopmostItem.IsChecked = (currentMode == DrawerPinMode.PinnedTopmost);
            MenuPinNormalItem.IsChecked = (currentMode == DrawerPinMode.PinnedNormal);
            MenuPinAutoHideItem.IsChecked = (currentMode == DrawerPinMode.AutoHide);
        }

        private void BallContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            if (!IsDrawerOpen && !IsMouseOver)
            {
                _collapseTimer.Stop();
                _collapseTimer.Start();
            }
        }

        private void MenuToggle_Click(object sender, RoutedEventArgs e)
        {
            BallClicked?.Invoke();
        }

        private void MenuAutoStart_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = MenuAutoStartItem.IsChecked;
            AutoStartService.SetAutoStart(isChecked);
            if (_settingsService != null)
            {
                _settingsService.Current.AutoStart = isChecked;
                _settingsService.Save();
            }
        }

        private void MenuPinTopmost_Click(object sender, RoutedEventArgs e)
        {
            App.Instance?.SetPinMode(DrawerPinMode.PinnedTopmost);
        }

        private void MenuPinNormal_Click(object sender, RoutedEventArgs e)
        {
            App.Instance?.SetPinMode(DrawerPinMode.PinnedNormal);
        }

        private void MenuPinAutoHide_Click(object sender, RoutedEventArgs e)
        {
            App.Instance?.SetPinMode(DrawerPinMode.AutoHide);
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsRequested?.Invoke();
        }

        private void MenuResetBall_Click(object sender, RoutedEventArgs e)
        {
            ResetBallPosition();
        }

        private void MenuOpenBrowser_Click(object sender, RoutedEventArgs e)
        {
            App.Instance?.OpenCurrentUrlInExternalBrowser();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            App.Instance?.ExitApp();
        }
    }
}

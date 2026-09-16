using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DeskSeek.Helpers;
using DeskSeek.Models;
using DeskSeek.Services;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using Cursors = System.Windows.Input.Cursors;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace DeskSeek.Views
{
    public partial class DrawerWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly WebViewManager _webViewManager;
        private readonly WindowResizer _windowResizer;

        private IntPtr _ballHwnd;
        private DrawerPinMode _pinMode = DrawerPinMode.AutoHide;
        private double _currentZoom = 0.9;
        private bool _isDockedToRight = true;
        private bool _isClosing;
        private bool _isOpening;

        public event Action<bool>? DrawerToggled;

        public bool IsOpen => Visibility == Visibility.Visible && !_isClosing;
        public bool IsPinned => _pinMode != DrawerPinMode.AutoHide;
        public DrawerPinMode PinMode => _pinMode;

        public DrawerWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;

            _webViewManager = new WebViewManager(WebBrowser, LoadingBar);
            _windowResizer = new WindowResizer(this, _settingsService, LeftResizeGrip, RightResizeGrip, BottomResizeGrip);
            _windowResizer.ResizeCompleted += () =>
            {
                WebSnapshotImage.Source = null;
            };

            SettingsControl.Initialize(_settingsService);
            SettingsControl.CloseRequested += CloseSettings;
            SettingsControl.ResetSizeRequested += ResetSidebarSize;

            DisclaimerControl.Accepted += OnDisclaimerAccepted;

            _currentZoom = _settingsService.Current.WebZoom > 0 ? _settingsService.Current.WebZoom : 0.9;
            SetPinMode(_settingsService.Current.PinMode, save: false);
            UpdateZoomUi();

            Loaded += (s, e) =>
            {
                _ = PreloadAsync();
                if (!_settingsService.Current.DisclaimerAccepted)
                {
                    ShowDisclaimer();
                }
            };
        }

        public async Task PreloadAsync()
        {
            await _webViewManager.InitializeAsync(_currentZoom);
        }

        public void SetBallWindowHandle(IntPtr hwnd)
        {
            _ballHwnd = hwnd;
        }

        #region Show, Hide and Smooth Fade Transitions

        public void ToggleDrawer(bool isDockedToRight)
        {
            if (IsOpen)
            {
                HideDrawer();
            }
            else
            {
                ShowDrawer(isDockedToRight);
            }
        }

        public void ShowDrawer(bool isDockedToRight)
        {
            _isDockedToRight = isDockedToRight;
            var wa = ScreenHelper.GetWorkArea(this);

            LeftResizeGrip.Visibility = _isDockedToRight ? Visibility.Visible : Visibility.Collapsed;
            RightResizeGrip.Visibility = _isDockedToRight ? Visibility.Collapsed : Visibility.Visible;

            var margin = _isDockedToRight ? new Thickness(8, 0, 0, 0) : new Thickness(0, 0, 8, 0);
            WebBrowser.Margin = margin;
            WebSnapshotImage.Margin = margin;

            double targetWidth = _settingsService.Current.DrawerWidth > 320
                ? _settingsService.Current.DrawerWidth
                : 460;
            Width = Math.Clamp(targetWidth, 340, Math.Min(1400, wa.Width * 0.85));

            double targetHeight = _settingsService.Current.DrawerHeight > 400
                ? _settingsService.Current.DrawerHeight
                : (wa.Height - 32);
            Height = Math.Clamp(targetHeight, 400, wa.Height - 16);

            // Restore Y position
            if (_settingsService.Current.RememberY && _settingsService.Current.LastDrawerY >= 0)
            {
                Top = Math.Clamp(_settingsService.Current.LastDrawerY, wa.Top, Math.Max(wa.Top, wa.Bottom - Height));
            }
            else
            {
                Top = wa.Top + 16;
            }

            // Restore X position
            double targetLeft;
            if (_settingsService.Current.RememberX && _settingsService.Current.LastDrawerX >= 0)
            {
                targetLeft = Math.Clamp(_settingsService.Current.LastDrawerX, wa.Left, Math.Max(wa.Left, wa.Right - Width));
                _isDockedToRight = (targetLeft + Width / 2) >= (wa.Left + wa.Right) / 2;
                LeftResizeGrip.Visibility = _isDockedToRight ? Visibility.Visible : Visibility.Collapsed;
                RightResizeGrip.Visibility = _isDockedToRight ? Visibility.Collapsed : Visibility.Visible;
                var dynamicMargin = _isDockedToRight ? new Thickness(8, 0, 0, 0) : new Thickness(0, 0, 8, 0);
                WebBrowser.Margin = dynamicMargin;
                WebSnapshotImage.Margin = dynamicMargin;
            }
            else
            {
                targetLeft = _isDockedToRight ? (wa.Right - Width + 4) : (wa.Left - 4);
            }

            Left = targetLeft;
            _isClosing = false;
            _isOpening = true;

            _webViewManager.Resume();

            bool useSnapshot = (WebSnapshotImage.Source != null &&
                                SettingsControl.Visibility != Visibility.Visible &&
                                DisclaimerControl.Visibility != Visibility.Visible &&
                                _settingsService.Current.DisclaimerAccepted);

            if (useSnapshot)
            {
                WebSnapshotImage.Visibility = Visibility.Visible;
                WebBrowser.Visibility = Visibility.Hidden;
            }
            else if (_settingsService.Current.DisclaimerAccepted && SettingsControl.Visibility != Visibility.Visible)
            {
                WebBrowser.Visibility = Visibility.Visible;
                WebSnapshotImage.Visibility = Visibility.Collapsed;
            }

            try
            {
                if (!_settingsService.Current.DisclaimerAccepted)
                {
                    ShowDisclaimer();
                }

                Topmost = (_pinMode != DrawerPinMode.PinnedNormal);
                Show();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        Activate();
                        if (_settingsService.Current.DisclaimerAccepted && !useSnapshot)
                        {
                            WebBrowser.Focus();
                        }
                    }
                    catch { }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeskSeek] ShowDrawer error: {ex.Message}");
            }

            CardTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            CardTranslate.X = 0;
            MainCard.Opacity = 0.0;

            var animFade = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            animFade.Completed += (s, e) =>
            {
                MainCard.BeginAnimation(UIElement.OpacityProperty, null);
                MainCard.Opacity = 1.0;
                _isOpening = false;
                DrawerToggled?.Invoke(true);

                if (_settingsService.Current.DisclaimerAccepted && SettingsControl.Visibility != Visibility.Visible)
                {
                    WebBrowser.Visibility = Visibility.Visible;
                    WebSnapshotImage.Visibility = Visibility.Collapsed;
                    try { WebBrowser.Focus(); } catch { }
                }
            };

            MainCard.BeginAnimation(UIElement.OpacityProperty, animFade);
        }

        public async void HideDrawer()
        {
            if (!IsOpen || _isClosing) return;

            _isClosing = true;
            _isOpening = false;

            if (_settingsService.Current.DisclaimerAccepted && SettingsControl.Visibility != Visibility.Visible)
            {
                var snapshot = await _webViewManager.CaptureSnapshotAsync();
                if (snapshot != null)
                {
                    WebSnapshotImage.Source = snapshot;
                    WebSnapshotImage.Visibility = Visibility.Visible;
                    WebBrowser.Visibility = Visibility.Hidden;
                }
            }

            var animFade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            animFade.Completed += async (s, e) =>
            {
                Hide();
                _isClosing = false;
                DrawerToggled?.Invoke(false);

                await _webViewManager.TrySuspendAsync();
            };

            MainCard.BeginAnimation(UIElement.OpacityProperty, animFade);
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            if (!IsPinned && IsOpen && !_isOpening && !_isClosing && !_windowResizer.IsResizing)
            {
                IntPtr fg = NativeMethods.GetForegroundWindow();
                if (_ballHwnd != IntPtr.Zero && fg == _ballHwnd)
                {
                    return;
                }

                HideDrawer();
            }
        }

        #endregion

        #region Pin Mode Management (3 Modes)

        public void SetPinMode(DrawerPinMode mode, bool save = true)
        {
            _pinMode = mode;
            if (save)
            {
                _settingsService.Current.PinMode = mode;
                _settingsService.Current.IsPinned = (mode != DrawerPinMode.AutoHide);
                _settingsService.Save();
            }

            switch (mode)
            {
                case DrawerPinMode.PinnedTopmost:
                    Topmost = true;
                    break;
                case DrawerPinMode.PinnedNormal:
                    Topmost = false;
                    break;
                case DrawerPinMode.AutoHide:
                default:
                    Topmost = true;
                    break;
            }

            UpdatePinUi();
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            DrawerPinMode nextMode = _pinMode switch
            {
                DrawerPinMode.PinnedTopmost => DrawerPinMode.PinnedNormal,
                DrawerPinMode.PinnedNormal => DrawerPinMode.AutoHide,
                DrawerPinMode.AutoHide => DrawerPinMode.PinnedTopmost,
                _ => DrawerPinMode.AutoHide
            };

            SetPinMode(nextMode);
        }

        private void PinButton_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PinContextMenu != null)
            {
                PinMenuTopmost.IsChecked = (_pinMode == DrawerPinMode.PinnedTopmost);
                PinMenuNormal.IsChecked = (_pinMode == DrawerPinMode.PinnedNormal);
                PinMenuAutoHide.IsChecked = (_pinMode == DrawerPinMode.AutoHide);
                PinContextMenu.PlacementTarget = PinButton;
                PinContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void PinMenuTopmost_Click(object sender, RoutedEventArgs e) => SetPinMode(DrawerPinMode.PinnedTopmost);
        private void PinMenuNormal_Click(object sender, RoutedEventArgs e) => SetPinMode(DrawerPinMode.PinnedNormal);
        private void PinMenuAutoHide_Click(object sender, RoutedEventArgs e) => SetPinMode(DrawerPinMode.AutoHide);

        private void UpdatePinUi()
        {
            switch (_pinMode)
            {
                case DrawerPinMode.PinnedTopmost:
                    PinButton.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x52, 0xD9));
                    PinIcon.Foreground = Brushes.White;
                    PinBadge.Visibility = Visibility.Visible;
                    PinBadge.Background = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
                    PinBadgeText.Text = "顶";
                    PinButton.ToolTip = "当前模式：常驻且置顶\n• 失焦不收起，窗口始终置顶最前\n• 左键点击切换为：常驻但不置顶\n• 右键点击可直接选择模式";
                    break;

                case DrawerPinMode.PinnedNormal:
                    PinButton.Background = new SolidColorBrush(Color.FromRgb(0xDB, 0xEA, 0xFE));
                    PinIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x52, 0xD9));
                    PinBadge.Visibility = Visibility.Visible;
                    PinBadge.Background = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
                    PinBadgeText.Text = "驻";
                    PinButton.ToolTip = "当前模式：常驻但不置顶\n• 失焦不收起，允许被其他窗口遮挡\n• 左键点击切换为：失焦自动收起\n• 右键点击可直接选择模式";
                    break;

                case DrawerPinMode.AutoHide:
                default:
                    PinButton.Background = Brushes.Transparent;
                    PinIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                    PinBadge.Visibility = Visibility.Collapsed;
                    PinButton.ToolTip = "当前模式：失焦自动收起\n• 随叫随到，点击其他地方时自动隐藏\n• 左键点击切换为：常驻且置顶\n• 右键点击可直接选择模式";
                    break;
            }
        }

        #endregion

        #region Header Toolbar Handlers

        private void ZoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (Math.Abs(_currentZoom - 0.9) < 0.01)
                _currentZoom = 0.8;
            else if (Math.Abs(_currentZoom - 0.8) < 0.01)
                _currentZoom = 1.0;
            else
                _currentZoom = 0.9;

            _settingsService.Current.WebZoom = _currentZoom;
            _settingsService.Save();

            UpdateZoomUi();
            _webViewManager.SetZoom(_currentZoom);
        }

        private void UpdateZoomUi()
        {
            ZoomText.Text = $"{Math.Round(_currentZoom * 100)}%";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _webViewManager.Reload();
        }

        private void ExternalBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            OpenCurrentInExternalBrowser();
        }

        public async void OpenCurrentInExternalBrowser()
        {
            try
            {
                string url = await _webViewManager.GetCurrentUrlAsync();
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeskSeek] Failed to open external browser: {ex.Message}");
                try
                {
                    Process.Start(new ProcessStartInfo("https://chat.deepseek.com") { UseShellExecute = true });
                }
                catch { }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HideDrawer();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                    UpdateSavedPosition();
                }
                catch { }
            }
        }

        private void UpdateSavedPosition()
        {
            if (_settingsService.Current.RememberX)
            {
                _settingsService.Current.LastDrawerX = Left;
            }
            if (_settingsService.Current.RememberY)
            {
                _settingsService.Current.LastDrawerY = Top;
            }
            _settingsService.Save();
        }

        #endregion

        #region Overlays: Settings and Disclaimer

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_settingsService.Current.DisclaimerAccepted)
                return;

            if (SettingsControl.Visibility == Visibility.Visible)
            {
                CloseSettings();
            }
            else
            {
                OpenSettings();
            }
        }

        public void OpenSettings()
        {
            SettingsControl.ReloadSettings();
            WebSnapshotImage.Visibility = Visibility.Collapsed;
            WebBrowser.Visibility = Visibility.Collapsed;
            SettingsControl.Visibility = Visibility.Visible;
        }

        public void CloseSettings()
        {
            SettingsControl.Visibility = Visibility.Collapsed;
            if (_settingsService.Current.DisclaimerAccepted)
            {
                WebSnapshotImage.Visibility = Visibility.Collapsed;
                WebBrowser.Visibility = Visibility.Visible;
            }
        }

        private void ResetSidebarSize()
        {
            var wa = ScreenHelper.GetWorkArea(this);
            double defaultWidth = 460;
            double defaultHeight = wa.Height - 32;

            Width = defaultWidth;
            Height = defaultHeight;
            Top = wa.Top + 16;
            Left = _isDockedToRight ? (wa.Right - Width + 4) : (wa.Left - 4);

            _settingsService.Current.DrawerWidth = defaultWidth;
            _settingsService.Current.DrawerHeight = defaultHeight;
            _settingsService.Current.LastDrawerX = -1;
            _settingsService.Current.LastDrawerY = -1;
            _settingsService.Save();
            WebSnapshotImage.Source = null;
        }

        private void ShowDisclaimer()
        {
            WebSnapshotImage.Visibility = Visibility.Collapsed;
            WebBrowser.Visibility = Visibility.Collapsed;
            SettingsControl.Visibility = Visibility.Collapsed;
            DisclaimerControl.Visibility = Visibility.Visible;
            DisclaimerControl.StartCountdown();
        }

        private void OnDisclaimerAccepted()
        {
            DisclaimerControl.Visibility = Visibility.Collapsed;
            WebSnapshotImage.Visibility = Visibility.Collapsed;
            WebBrowser.Visibility = Visibility.Visible;
            try { WebBrowser.Focus(); } catch { }
        }

        #endregion
    }
}

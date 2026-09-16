using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;
using DeskSeek.Helpers;
using DeskSeek.Services;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Border = System.Windows.Controls.Border;
using Cursors = System.Windows.Input.Cursors;

namespace DeskSeek.Views
{
    public partial class DrawerWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private readonly SettingsService _settingsService;
        private IntPtr _ballHwnd;
        private bool _isPinned;
        private bool _isClosing;
        private bool _isOpening;
        private bool _isInitialized;
        private double _currentZoom = 0.9;
        private bool _isDockedToRight = true;

        // Resize state variables
        private bool _isResizingWidth;
        private bool _isResizingHeight;
        private Point _resizeStartMouseScreen;
        private double _resizeStartWidth;
        private double _resizeStartHeight;

        // First-launch disclaimer state
        private System.Windows.Threading.DispatcherTimer? _disclaimerTimer;
        private int _disclaimerCountdown = 3;

        public event Action<bool>? DrawerToggled;
        public bool IsOpen => Visibility == Visibility.Visible && !_isClosing;

        public DrawerWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;

            _isPinned = _settingsService.Current.IsPinned;
            _currentZoom = _settingsService.Current.WebZoom > 0 ? _settingsService.Current.WebZoom : 0.9;

            UpdatePinUi();
            UpdateZoomUi();

            Loaded += (s, e) =>
            {
                _ = PreloadAsync();
                if (!_settingsService.Current.DisclaimerAccepted)
                {
                    ShowDisclaimerOverlay();
                }
            };
        }

        public async System.Threading.Tasks.Task PreloadAsync()
        {
            if (_isInitialized) return;

            try
            {
                var profileDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DeskSeek", "WebViewProfile");
                Directory.CreateDirectory(profileDir);

                // Configure performance flags to optimize memory and disable bloated background network tasks
                var options = new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--disable-features=Translate,OptimizationHints,MediaRouter --disable-background-networking --disable-sync --disable-component-update --renderer-process-limit=1 --js-flags=\"--max-old-space-size=256\""
                };

                var env = await CoreWebView2Environment.CreateAsync(null, profileDir, options);
                await WebBrowser.EnsureCoreWebView2Async(env);

                _isInitialized = true;
                WebBrowser.DefaultBackgroundColor = System.Drawing.Color.FromArgb(248, 250, 252);
                WebBrowser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                WebBrowser.CoreWebView2.Settings.AreDevToolsEnabled = true;

                // Open external links in default system browser
                WebBrowser.CoreWebView2.NewWindowRequested += (s, args) =>
                {
                    args.Handled = true;
                    try
                    {
                        Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true });
                    }
                    catch { }
                };

                // Navigation status
                WebBrowser.NavigationStarting += (s, args) =>
                {
                    LoadingBar.Visibility = Visibility.Visible;
                };

                WebBrowser.NavigationCompleted += (s, args) =>
                {
                    LoadingBar.Visibility = Visibility.Collapsed;
                    try
                    {
                        WebBrowser.ZoomFactor = _currentZoom;
                    }
                    catch { }

                    // Inject custom scrollbar style for sleek sidebar
                    string script = @"
                        (function() {
                            if (document.getElementById('deskseek-custom-style')) return;
                            const style = document.createElement('style');
                            style.id = 'deskseek-custom-style';
                            style.textContent = `
                                ::-webkit-scrollbar { width: 6px; height: 6px; }
                                ::-webkit-scrollbar-thumb { background: rgba(0,0,0,0.18); border-radius: 3px; }
                                ::-webkit-scrollbar-thumb:hover { background: rgba(0,0,0,0.32); }
                            `;
                            document.head.appendChild(style);
                        })();
                    ";
                    WebBrowser.CoreWebView2.ExecuteScriptAsync(script);
                };

                WebBrowser.Source = new Uri("https://chat.deepseek.com");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeskSeek] WebView2 init error: {ex.Message}");
                LoadingBar.Visibility = Visibility.Collapsed;
            }
        }

        public void SetBallWindowHandle(IntPtr hwnd)
        {
            _ballHwnd = hwnd;
        }

        public void ShowDrawer(bool isDockedToRight)
        {
            _isDockedToRight = isDockedToRight;
            var wa = ScreenHelper.GetWorkArea(this);

            // Configure active resize grip based on docked side
            LeftResizeGrip.Visibility = _isDockedToRight ? Visibility.Visible : Visibility.Collapsed;
            RightResizeGrip.Visibility = _isDockedToRight ? Visibility.Collapsed : Visibility.Visible;

            double targetWidth = _settingsService.Current.DrawerWidth > 320
                ? _settingsService.Current.DrawerWidth
                : 460;
            Width = Math.Clamp(targetWidth, 340, Math.Min(1400, wa.Width * 0.85));

            double targetHeight = _settingsService.Current.DrawerHeight > 400
                ? _settingsService.Current.DrawerHeight
                : (wa.Height - 32);
            Height = Math.Clamp(targetHeight, 400, wa.Height - 16);
            Top = wa.Top + 16;

            double targetLeft;
            double startX;

            if (_isDockedToRight)
            {
                targetLeft = wa.Right - Width + 4;
                startX = Width; // Slide in from right
            }
            else
            {
                targetLeft = wa.Left - 4;
                startX = -Width; // Slide in from left
            }

            Left = targetLeft;
            _isClosing = false;
            _isOpening = true;

            // Prepare initial GPU transform state
            CardTranslate.X = startX;
            MainCard.Opacity = 0.2;

            // Resume WebView2 if it was suspended
            if (WebBrowser.CoreWebView2 != null)
            {
                try
                {
                    WebBrowser.CoreWebView2.Resume();
                }
                catch { }
            }

            try
            {
                if (!_settingsService.Current.DisclaimerAccepted)
                {
                    ShowDisclaimerOverlay();
                }

                Show();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        Activate();
                        if (_settingsService.Current.DisclaimerAccepted)
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

            // Silky GPU-accelerated quintic deceleration slide
            var animSlide = new DoubleAnimation(startX, 0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            };
            var animFade = new DoubleAnimation(0.2, 1.0, TimeSpan.FromMilliseconds(240))
            {
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };

            animSlide.Completed += (s, e) =>
            {
                CardTranslate.X = 0;
                MainCard.Opacity = 1.0;
                _isOpening = false;
                DrawerToggled?.Invoke(true);
            };

            CardTranslate.BeginAnimation(TranslateTransform.XProperty, animSlide);
            MainCard.BeginAnimation(UIElement.OpacityProperty, animFade);
        }

        public void HideDrawer()
        {
            if (_isClosing || Visibility != Visibility.Visible)
                return;

            _isClosing = true;
            _isOpening = false;

            double endX = _isDockedToRight ? Width : -Width;

            // Silky smooth slide out
            var animSlide = new DoubleAnimation(CardTranslate.X, endX, TimeSpan.FromMilliseconds(240))
            {
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
            };
            var animFade = new DoubleAnimation(MainCard.Opacity, 0.0, TimeSpan.FromMilliseconds(200));

            animSlide.Completed += (s, e) =>
            {
                Hide();
                _isClosing = false;
                DrawerToggled?.Invoke(false);

                // Suspend background WebView2 if user enabled it, drastically freeing RAM and CPU
                if (_settingsService.Current.SuspendWhenHidden && WebBrowser.CoreWebView2 != null)
                {
                    try
                    {
                        _ = WebBrowser.CoreWebView2.TrySuspendAsync();
                    }
                    catch { }
                }
            };

            CardTranslate.BeginAnimation(TranslateTransform.XProperty, animSlide);
            MainCard.BeginAnimation(UIElement.OpacityProperty, animFade);
        }

        public void ToggleDrawer(bool isDockedToRight)
        {
            // Debounce toggling while in motion
            if (_isOpening || _isClosing)
                return;

            if (IsOpen)
            {
                HideDrawer();
            }
            else
            {
                ShowDrawer(isDockedToRight);
            }
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            if (!_isPinned && IsOpen && !_isOpening && !_isClosing && !_isResizingWidth && !_isResizingHeight)
            {
                // Ignore deactivation if the user clicked the floating ball
                IntPtr fg = GetForegroundWindow();
                if (_ballHwnd != IntPtr.Zero && fg == _ballHwnd)
                {
                    return;
                }

                HideDrawer();
            }
        }

        #region Settings Overlay Management

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_settingsService.Current.DisclaimerAccepted)
                return; // Must accept disclaimer before accessing settings

            if (SettingsOverlay.Visibility == Visibility.Visible)
            {
                CloseSettings();
            }
            else
            {
                OpenSettings();
            }
        }

        private void OpenSettings()
        {
            // Sync settings state to UI
            SettingAutoStart.IsChecked = AutoStartService.IsAutoStartEnabled();
            SettingPin.IsChecked = _settingsService.Current.IsPinned;
            SettingAutoCollapse.IsChecked = _settingsService.Current.AutoCollapse;
            SettingSuspend.IsChecked = _settingsService.Current.SuspendWhenHidden;

            // Select current hotkey
            string currentHk = _settingsService.Current.Hotkey;
            foreach (ComboBoxItem item in SettingHotkeyCombo.Items)
            {
                if (string.Equals(item.Content?.ToString(), currentHk, StringComparison.OrdinalIgnoreCase))
                {
                    SettingHotkeyCombo.SelectedItem = item;
                    break;
                }
            }

            // Hide WebBrowser HwndHost to completely eliminate WPF airspace collision
            WebBrowser.Visibility = Visibility.Collapsed;
            SettingsOverlay.Visibility = Visibility.Visible;
        }

        private void CloseSettings()
        {
            SettingsOverlay.Visibility = Visibility.Collapsed;
            if (_settingsService.Current.DisclaimerAccepted)
            {
                WebBrowser.Visibility = Visibility.Visible;
            }
        }

        private void BackFromSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            CloseSettings();
        }

        #endregion

        #region Disclaimer Overlay Management

        private void ShowDisclaimerOverlay()
        {
            WebBrowser.Visibility = Visibility.Collapsed;
            SettingsOverlay.Visibility = Visibility.Collapsed;
            DisclaimerOverlay.Visibility = Visibility.Visible;

            if (_disclaimerTimer != null) return;

            _disclaimerCountdown = 3;
            DisclaimerCheckBox.IsEnabled = false;
            DisclaimerCheckBox.IsChecked = false;
            DisclaimerAgreeButton.IsEnabled = false;
            DisclaimerAgreeButton.Background = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
            DisclaimerAgreeButton.Cursor = Cursors.No;
            DisclaimerButtonText.Text = $"请先阅读免责声明 ({_disclaimerCountdown}s)";

            _disclaimerTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _disclaimerTimer.Tick += (s, e) =>
            {
                _disclaimerCountdown--;
                if (_disclaimerCountdown > 0)
                {
                    DisclaimerButtonText.Text = $"请先阅读免责声明 ({_disclaimerCountdown}s)";
                }
                else
                {
                    _disclaimerTimer.Stop();
                    _disclaimerTimer = null;
                    DisclaimerCheckBox.IsEnabled = true;
                    DisclaimerButtonText.Text = "请勾选已阅读同意";
                }
            };
            _disclaimerTimer.Start();
        }

        private void DisclaimerCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = DisclaimerCheckBox.IsChecked == true;
            if (isChecked)
            {
                DisclaimerAgreeButton.IsEnabled = true;
                DisclaimerAgreeButton.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x52, 0xD9));
                DisclaimerAgreeButton.Cursor = Cursors.Hand;
                DisclaimerButtonText.Text = "同意并开始使用";
            }
            else
            {
                DisclaimerAgreeButton.IsEnabled = false;
                DisclaimerAgreeButton.Background = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                DisclaimerAgreeButton.Cursor = Cursors.No;
                DisclaimerButtonText.Text = _disclaimerCountdown > 0
                    ? $"请先阅读免责声明 ({_disclaimerCountdown}s)"
                    : "请勾选已阅读同意";
            }
        }

        private void DisclaimerAgreeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DisclaimerCheckBox.IsChecked != true) return;

            _disclaimerTimer?.Stop();
            _disclaimerTimer = null;

            _settingsService.Current.DisclaimerAccepted = true;
            _settingsService.Save();

            DisclaimerOverlay.Visibility = Visibility.Collapsed;
            WebBrowser.Visibility = Visibility.Visible;
            try
            {
                WebBrowser.Focus();
            }
            catch { }
        }

        #endregion

        private void SettingAutoStart_Click(object sender, RoutedEventArgs e)
        {
            bool val = SettingAutoStart.IsChecked == true;
            AutoStartService.SetAutoStart(val);
            _settingsService.Current.AutoStart = val;
            _settingsService.Save();
        }

        private void SettingPin_Click(object sender, RoutedEventArgs e)
        {
            bool val = SettingPin.IsChecked == true;
            _isPinned = val;
            _settingsService.Current.IsPinned = val;
            _settingsService.Save();
            UpdatePinUi();
        }

        private void SettingAutoCollapse_Click(object sender, RoutedEventArgs e)
        {
            bool val = SettingAutoCollapse.IsChecked == true;
            _settingsService.Current.AutoCollapse = val;
            _settingsService.Save();
        }

        private void SettingHotkeyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingHotkeyCombo.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                string hk = item.Content.ToString()!;
                _settingsService.Current.Hotkey = hk;
                _settingsService.Save();
                App.Instance?.ReloadHotkey(hk);
            }
        }

        private void SettingSuspend_Click(object sender, RoutedEventArgs e)
        {
            bool val = SettingSuspend.IsChecked == true;
            _settingsService.Current.SuspendWhenHidden = val;
            _settingsService.Save();
        }

        private void ResetSizeButton_Click(object sender, RoutedEventArgs e)
        {
            var wa = ScreenHelper.GetWorkArea(this);
            double defaultWidth = 460;
            double defaultHeight = wa.Height - 32;

            Width = defaultWidth;
            Height = defaultHeight;
            Top = wa.Top + 16;

            if (_isDockedToRight)
            {
                Left = wa.Right - Width + 4;
            }
            else
            {
                Left = wa.Left - 4;
            }

            _settingsService.Current.DrawerWidth = defaultWidth;
            _settingsService.Current.DrawerHeight = defaultHeight;
            _settingsService.Save();
        }

        private void OpenDataDirButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DeskSeek");
                Directory.CreateDirectory(dir);
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            }
            catch { }
        }

        #region Width & Height Resizing Handlers

        private void ResizeGrip_MouseEnter(object sender, MouseEventArgs e)
        {
            var grip = sender as FrameworkElement;
            if (grip?.FindName("LeftResizeBar") is Border leftBar)
            {
                leftBar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.85, TimeSpan.FromMilliseconds(150)));
            }
            if (grip?.FindName("RightResizeBar") is Border rightBar)
            {
                rightBar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.85, TimeSpan.FromMilliseconds(150)));
            }
        }

        private void ResizeGrip_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isResizingWidth) return;
            var grip = sender as FrameworkElement;
            if (grip?.FindName("LeftResizeBar") is Border leftBar)
            {
                leftBar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(150)));
            }
            if (grip?.FindName("RightResizeBar") is Border rightBar)
            {
                rightBar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(150)));
            }
        }

        private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isResizingWidth = true;
            var grip = (Border)sender;
            grip.CaptureMouse();
            _resizeStartMouseScreen = PointToScreen(e.GetPosition(this));
            _resizeStartWidth = Width;
        }

        private void ResizeGrip_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizingWidth) return;

            Point currentScreen = PointToScreen(e.GetPosition(this));
            double deltaX = currentScreen.X - _resizeStartMouseScreen.X;
            var wa = ScreenHelper.GetWorkArea(this);
            double maxAllowedWidth = Math.Min(1400, wa.Width * 0.85);

            if (_isDockedToRight)
            {
                // Dragging left increases width
                double newWidth = Math.Clamp(_resizeStartWidth - deltaX, 340, maxAllowedWidth);
                Width = newWidth;
                Left = wa.Right - newWidth + 4;
            }
            else
            {
                // Dragging right increases width
                double newWidth = Math.Clamp(_resizeStartWidth + deltaX, 340, maxAllowedWidth);
                Width = newWidth;
            }

            _settingsService.Current.DrawerWidth = Width;
        }

        private void ResizeGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizingWidth)
            {
                _isResizingWidth = false;
                ((Border)sender).ReleaseMouseCapture();
                _settingsService.Save();

                ResizeGrip_MouseLeave(sender, e);
            }
        }

        private void BottomResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isResizingHeight = true;
            ((Border)sender).CaptureMouse();
            _resizeStartMouseScreen = PointToScreen(e.GetPosition(this));
            _resizeStartHeight = Height;
        }

        private void BottomResizeGrip_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizingHeight) return;

            Point currentScreen = PointToScreen(e.GetPosition(this));
            double deltaY = currentScreen.Y - _resizeStartMouseScreen.Y;
            var wa = ScreenHelper.GetWorkArea(this);

            double newHeight = Math.Clamp(_resizeStartHeight + deltaY, 400, wa.Height - 16);
            Height = newHeight;
            _settingsService.Current.DrawerHeight = Height;
        }

        private void BottomResizeGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizingHeight)
            {
                _isResizingHeight = false;
                ((Border)sender).ReleaseMouseCapture();
                _settingsService.Save();
            }
        }

        #endregion

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            _isPinned = !_isPinned;
            _settingsService.Current.IsPinned = _isPinned;
            _settingsService.Save();
            UpdatePinUi();
        }

        private void UpdatePinUi()
        {
            if (_isPinned)
            {
                PinButton.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x52, 0xD9));
                PinIcon.Foreground = Brushes.White;
                PinButton.ToolTip = "已固定常驻 (点击取消固定)";
            }
            else
            {
                PinButton.Background = Brushes.Transparent;
                PinIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
                PinButton.ToolTip = "随叫随到模式 (失焦自动收起)";
            }
        }

        private void ZoomButton_Click(object sender, RoutedEventArgs e)
        {
            // Cycle 0.9 -> 0.8 -> 1.0 -> 0.9
            if (Math.Abs(_currentZoom - 0.9) < 0.01)
                _currentZoom = 0.8;
            else if (Math.Abs(_currentZoom - 0.8) < 0.01)
                _currentZoom = 1.0;
            else
                _currentZoom = 0.9;

            _settingsService.Current.WebZoom = _currentZoom;
            _settingsService.Save();

            UpdateZoomUi();

            if (WebBrowser.CoreWebView2 != null)
            {
                try
                {
                    WebBrowser.ZoomFactor = _currentZoom;
                }
                catch { }
            }
        }

        private void UpdateZoomUi()
        {
            ZoomText.Text = $"{Math.Round(_currentZoom * 100)}%";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            WebBrowser.CoreWebView2?.Reload();
        }

        private void ExternalBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://chat.deepseek.com") { UseShellExecute = true });
            }
            catch { }
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
                }
                catch { }
            }
        }
    }
}

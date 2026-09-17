using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using DeskSeek.Helpers;
using DeskSeek.Models;
using DeskSeek.Services;
using DeskSeek.Views;
using Application = System.Windows.Application;

namespace DeskSeek
{
    public partial class App : Application
    {
        private const string MutexName = @"Local\DeskSeek_SingleInstance_Mutex";
        private Mutex? _instanceMutex;
        private NotifyIcon? _trayIcon;

        private SettingsService? _settingsService;
        private HotkeyService? _hotkeyService;
        private FloatingBallWindow? _ballWindow;
        private DrawerWindow? _drawerWindow;

        private static readonly uint SummonWindowMessage = NativeMethods.RegisterWindowMessage("DeskSeek_Summon_Instance_Msg");

        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += (s, args) =>
            {
                Debug.WriteLine($"[DeskSeek] Dispatcher Unhandled Exception: {args.Exception}");
                args.Handled = true; // Prevent application crash
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Debug.WriteLine($"[DeskSeek] AppDomain Unhandled Exception: {args.ExceptionObject}");
            };

            // Single-instance check
            _instanceMutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                // Wake up and summon existing instance to screen
                NativeMethods.PostMessage((IntPtr)NativeMethods.HWND_BROADCAST, SummonWindowMessage, IntPtr.Zero, IntPtr.Zero);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            _settingsService = new SettingsService();
            LocalizationService.Instance.Initialize(_settingsService.Current.Language);

            // Initialize Windows
            _ballWindow = new FloatingBallWindow(_settingsService);
            _drawerWindow = new DrawerWindow(_settingsService);
            _ = _drawerWindow.PreloadAsync();

            IntPtr ballHwnd = new System.Windows.Interop.WindowInteropHelper(_ballWindow).EnsureHandle();
            _drawerWindow.SetBallWindowHandle(ballHwnd);

            // Listen for second-instance wake up message
            var hwndSource = System.Windows.Interop.HwndSource.FromHwnd(ballHwnd);
            hwndSource?.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if ((uint)msg == SummonWindowMessage)
                {
                    _drawerWindow?.ShowDrawer(_ballWindow.IsCurrentlyOnRightSide());
                    handled = true;
                }
                return IntPtr.Zero;
            });

            // Wire up event interactions
            _ballWindow.BallClicked += () =>
            {
                _drawerWindow.ToggleDrawer(_ballWindow.IsCurrentlyOnRightSide());
            };

            _ballWindow.SettingsRequested += () =>
            {
                _drawerWindow.ShowDrawer(_ballWindow.IsCurrentlyOnRightSide());
                _drawerWindow.OpenSettings();
            };

            _drawerWindow.DrawerToggled += (isOpen) =>
            {
                _ballWindow.NotifyDrawerStateChanged(isOpen);
            };

            _ballWindow.Show();

            // Initialize Hotkey
            _hotkeyService = new HotkeyService();
            _hotkeyService.HotkeyPressed += () =>
            {
                _drawerWindow.ToggleDrawer(_ballWindow.IsCurrentlyOnRightSide());
            };
            _hotkeyService.RegisterByString(_ballWindow, _settingsService.Current.Hotkey);

            // Initialize Tray Icon
            InitTrayIcon();

            // On first launch, automatically open the drawer so user sees the disclaimer immediately
            if (!_settingsService.Current.DisclaimerAccepted)
            {
                _drawerWindow.ShowDrawer(_ballWindow.IsCurrentlyOnRightSide());
            }
        }

        public static App? Instance => Current as App;

        public bool ReloadHotkey(string newHotkey)
        {
            if (_hotkeyService != null && _ballWindow != null)
            {
                bool success = _hotkeyService.RegisterByString(_ballWindow, newHotkey);
                UpdateTrayTooltip();
                return success;
            }
            return false;
        }

        private ToolStripMenuItem? _trayToggleItem;
        private ToolStripMenuItem? _trayAutoStartItem;
        private ToolStripMenuItem? _trayPinTopmostItem;
        private ToolStripMenuItem? _trayPinNormalItem;
        private ToolStripMenuItem? _trayPinAutoHideItem;
        private ToolStripMenuItem? _traySettingsItem;
        private ToolStripMenuItem? _trayResetItem;
        private ToolStripMenuItem? _trayOpenBrowserItem;
        private ToolStripMenuItem? _trayExitItem;

        public void UpdateTrayTooltip()
        {
            if (_trayIcon == null) return;
            string hk = _settingsService?.Current.Hotkey ?? "Alt+D";
            string prefix = LocalizationService.Instance.GetString("Lang_TrayTooltipPrefix");
            string text = (string.IsNullOrWhiteSpace(hk) || hk == "无" || hk.Equals("None", StringComparison.OrdinalIgnoreCase))
                ? $"{prefix} ({LocalizationService.Instance.GetString("Lang_TrayNoHotkey")})"
                : $"{prefix} ({hk})";

            if (text.Length > 63)
            {
                text = text.Substring(0, 63);
            }
            _trayIcon.Text = text;
        }

        public void UpdateTrayMenuTexts()
        {
            if (_trayToggleItem != null)
            {
                string hk = _settingsService?.Current.Hotkey ?? "Alt+D";
                string baseToggle = LocalizationService.Instance.GetString("Lang_MenuToggle");
                if (string.IsNullOrWhiteSpace(hk) || hk == "无" || hk.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    _trayToggleItem.Text = baseToggle;
                }
                else
                {
                    _trayToggleItem.Text = $"{baseToggle} ({hk})";
                }
            }

            if (_trayAutoStartItem != null) _trayAutoStartItem.Text = LocalizationService.Instance.GetString("Lang_MenuAutoStart");
            if (_trayPinTopmostItem != null) _trayPinTopmostItem.Text = LocalizationService.Instance.GetString("Lang_PinTopmostHeader");
            if (_trayPinNormalItem != null) _trayPinNormalItem.Text = LocalizationService.Instance.GetString("Lang_PinNormalHeader");
            if (_trayPinAutoHideItem != null) _trayPinAutoHideItem.Text = LocalizationService.Instance.GetString("Lang_PinAutoHideHeader");
            if (_traySettingsItem != null) _traySettingsItem.Text = LocalizationService.Instance.GetString("Lang_MenuSettings");
            if (_trayResetItem != null) _trayResetItem.Text = LocalizationService.Instance.GetString("Lang_MenuResetBall");
            if (_trayOpenBrowserItem != null) _trayOpenBrowserItem.Text = LocalizationService.Instance.GetString("Lang_MenuOpenBrowser");
            if (_trayExitItem != null) _trayExitItem.Text = LocalizationService.Instance.GetString("Lang_MenuExit");

            UpdateTrayTooltip();
        }

        public void SetPinMode(DrawerPinMode mode)
        {
            if (_drawerWindow != null)
            {
                _drawerWindow.SetPinMode(mode);
            }
            else if (_settingsService != null)
            {
                _settingsService.Current.PinMode = mode;
                _settingsService.Current.IsPinned = (mode != DrawerPinMode.AutoHide);
                _settingsService.Save();
            }
        }

        public void OpenCurrentUrlInExternalBrowser()
        {
            if (_drawerWindow != null)
            {
                _drawerWindow.OpenCurrentInExternalBrowser();
            }
            else
            {
                try
                {
                    Process.Start(new ProcessStartInfo("https://chat.deepseek.com") { UseShellExecute = true });
                }
                catch { }
            }
        }

        private void InitTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Visible = true,
                Icon = GenerateAppIcon()
            };

            var contextMenu = new ContextMenuStrip();

            _trayToggleItem = new ToolStripMenuItem("显示 / 隐藏", null, (s, e) =>
            {
                if (_ballWindow != null && _drawerWindow != null)
                {
                    _drawerWindow.ToggleDrawer(_ballWindow.IsCurrentlyOnRightSide());
                }
            })
            {
                Font = new Font(contextMenu.Font, System.Drawing.FontStyle.Bold)
            };
            contextMenu.Items.Add(_trayToggleItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Auto-start with Windows toggle
            _trayAutoStartItem = new ToolStripMenuItem("开机自启")
            {
                Checked = AutoStartService.IsAutoStartEnabled(),
                CheckOnClick = true
            };
            _trayAutoStartItem.Click += (s, e) =>
            {
                AutoStartService.SetAutoStart(_trayAutoStartItem.Checked);
                if (_settingsService != null)
                {
                    _settingsService.Current.AutoStart = _trayAutoStartItem.Checked;
                    _settingsService.Save();
                }
            };
            contextMenu.Items.Add(_trayAutoStartItem);

            // Pin modes
            _trayPinTopmostItem = new ToolStripMenuItem("常驻且置顶", null, (s, e) => SetPinMode(DrawerPinMode.PinnedTopmost));
            _trayPinNormalItem = new ToolStripMenuItem("常驻但不置顶", null, (s, e) => SetPinMode(DrawerPinMode.PinnedNormal));
            _trayPinAutoHideItem = new ToolStripMenuItem("失焦自动收起", null, (s, e) => SetPinMode(DrawerPinMode.AutoHide));

            contextMenu.Items.Add(_trayPinTopmostItem);
            contextMenu.Items.Add(_trayPinNormalItem);
            contextMenu.Items.Add(_trayPinAutoHideItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Settings
            _traySettingsItem = new ToolStripMenuItem("偏好设置...", null, (s, e) =>
            {
                if (_ballWindow != null && _drawerWindow != null)
                {
                    _drawerWindow.ShowDrawer(_ballWindow.IsCurrentlyOnRightSide());
                    _drawerWindow.OpenSettings();
                }
            });
            contextMenu.Items.Add(_traySettingsItem);

            // Reset ball position
            _trayResetItem = new ToolStripMenuItem("重置悬浮球位置", null, (s, e) =>
            {
                _ballWindow?.ResetBallPosition();
            });
            contextMenu.Items.Add(_trayResetItem);

            // Open in browser
            _trayOpenBrowserItem = new ToolStripMenuItem("在浏览器中打开当前对话", null, (s, e) =>
            {
                OpenCurrentUrlInExternalBrowser();
            });
            contextMenu.Items.Add(_trayOpenBrowserItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Exit
            _trayExitItem = new ToolStripMenuItem("退出 DeskSeek", null, (s, e) =>
            {
                ExitApp();
            });
            contextMenu.Items.Add(_trayExitItem);

            contextMenu.Opening += (s, e) =>
            {
                UpdateTrayMenuTexts();

                if (_trayAutoStartItem != null) _trayAutoStartItem.Checked = AutoStartService.IsAutoStartEnabled();
                var currentMode = _drawerWindow?.PinMode ?? _settingsService?.Current.PinMode ?? DrawerPinMode.AutoHide;
                if (_trayPinTopmostItem != null) _trayPinTopmostItem.Checked = (currentMode == DrawerPinMode.PinnedTopmost);
                if (_trayPinNormalItem != null) _trayPinNormalItem.Checked = (currentMode == DrawerPinMode.PinnedNormal);
                if (_trayPinAutoHideItem != null) _trayPinAutoHideItem.Checked = (currentMode == DrawerPinMode.AutoHide);
            };

            LocalizationService.Instance.LanguageChanged += (lang) =>
            {
                UpdateTrayMenuTexts();
            };

            UpdateTrayMenuTexts();

            _trayIcon.ContextMenuStrip = contextMenu;

            _trayIcon.DoubleClick += (s, e) =>
            {
                if (_ballWindow != null && _drawerWindow != null)
                {
                    _drawerWindow.ToggleDrawer(_ballWindow.IsCurrentlyOnRightSide());
                }
            };
        }

        private Icon GenerateAppIcon()
        {
            try
            {
                // 1. Try loading directly from embedded WPF Pack URI resource
                var icoUri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
                var streamInfo = System.Windows.Application.GetResourceStream(icoUri);
                if (streamInfo?.Stream != null)
                {
                    using (streamInfo.Stream)
                    {
                        return new Icon(streamInfo.Stream, SystemInformation.SmallIconSize);
                    }
                }
            }
            catch { }

            try
            {
                // 2. Fallback to physical file if exists
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string icoPath = Path.Combine(baseDir, "Assets", "app.ico");
                if (File.Exists(icoPath))
                {
                    return new Icon(icoPath, SystemInformation.SmallIconSize);
                }

                // 3. Fallback to embedded logo.png
                var pngUri = new Uri("pack://application:,,,/Assets/logo.png", UriKind.Absolute);
                var pngStream = System.Windows.Application.GetResourceStream(pngUri);
                if (pngStream?.Stream != null)
                {
                    using (pngStream.Stream)
                    using (var src = System.Drawing.Image.FromStream(pngStream.Stream))
                    {
                        int size = Math.Max(16, SystemInformation.SmallIconSize.Width);
                        using var bmp = new Bitmap(size, size);
                        using var g = Graphics.FromImage(bmp);
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.Clear(Color.Transparent);
                        g.DrawImage(src, new Rectangle(0, 0, size, size), 120, 130, 800, 800, GraphicsUnit.Pixel);
                        return Icon.FromHandle(bmp.GetHicon());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeskSeek] Failed to load custom tray icon: {ex.Message}");
            }

            return SystemIcons.Application;
        }

        public void ExitApp()
        {
            _trayIcon?.Dispose();
            _trayIcon = null;

            _hotkeyService?.Dispose();
            _hotkeyService = null;

            _ballWindow?.Close();
            _drawerWindow?.Close();

            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _hotkeyService?.Dispose();

            if (_instanceMutex != null)
            {
                _instanceMutex.ReleaseMutex();
                _instanceMutex.Dispose();
                _instanceMutex = null;
            }

            base.OnExit(e);
        }
    }
}

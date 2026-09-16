using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
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
                System.Windows.MessageBox.Show(
                    "DeskSeek 已经在运行中！可通过贴边悬浮球、快捷键 Alt+D 或右下角托盘图标唤出。",
                    "DeskSeek 已启动",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            _settingsService = new SettingsService();

            // Initialize Windows
            _ballWindow = new FloatingBallWindow(_settingsService);
            _drawerWindow = new DrawerWindow(_settingsService);
            _ = _drawerWindow.PreloadAsync();

            _drawerWindow.SetBallWindowHandle(new System.Windows.Interop.WindowInteropHelper(_ballWindow).EnsureHandle());

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

        public void UpdateTrayTooltip()
        {
            if (_trayIcon == null) return;
            string hk = _settingsService?.Current.Hotkey ?? "Alt+D";
            if (string.IsNullOrWhiteSpace(hk) || hk == "无" || hk.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                _trayIcon.Text = "DeskSeek - DeepSeek 桌面轻量吸附窗 (未设置快捷键)";
            }
            else
            {
                _trayIcon.Text = $"DeskSeek - DeepSeek 桌面轻量吸附窗 ({hk})";
            }
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
            UpdateTrayTooltip();

            var contextMenu = new ContextMenuStrip();

            var toggleItem = new ToolStripMenuItem("显示 / 隐藏 (Alt+D)", null, (s, e) =>
            {
                if (_ballWindow != null && _drawerWindow != null)
                {
                    _drawerWindow.ToggleDrawer(_ballWindow.IsCurrentlyOnRightSide());
                }
            })
            {
                Font = new Font(contextMenu.Font, System.Drawing.FontStyle.Bold)
            };
            contextMenu.Items.Add(toggleItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Auto-start with Windows toggle
            var autoStartItem = new ToolStripMenuItem("开机自启")
            {
                Checked = AutoStartService.IsAutoStartEnabled(),
                CheckOnClick = true
            };
            autoStartItem.Click += (s, e) =>
            {
                AutoStartService.SetAutoStart(autoStartItem.Checked);
                if (_settingsService != null)
                {
                    _settingsService.Current.AutoStart = autoStartItem.Checked;
                    _settingsService.Save();
                }
            };
            contextMenu.Items.Add(autoStartItem);

            // Pin modes
            var pinTopmostItem = new ToolStripMenuItem("常驻且置顶", null, (s, e) => SetPinMode(DrawerPinMode.PinnedTopmost));
            var pinNormalItem = new ToolStripMenuItem("常驻但不置顶", null, (s, e) => SetPinMode(DrawerPinMode.PinnedNormal));
            var pinAutoHideItem = new ToolStripMenuItem("失焦自动收起", null, (s, e) => SetPinMode(DrawerPinMode.AutoHide));

            contextMenu.Items.Add(pinTopmostItem);
            contextMenu.Items.Add(pinNormalItem);
            contextMenu.Items.Add(pinAutoHideItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Settings
            var settingsItem = new ToolStripMenuItem("偏好设置...", null, (s, e) =>
            {
                if (_ballWindow != null && _drawerWindow != null)
                {
                    _drawerWindow.ShowDrawer(_ballWindow.IsCurrentlyOnRightSide());
                    _drawerWindow.OpenSettings();
                }
            });
            contextMenu.Items.Add(settingsItem);

            // Reset ball position
            var resetItem = new ToolStripMenuItem("重置悬浮球位置", null, (s, e) =>
            {
                _ballWindow?.ResetBallPosition();
            });
            contextMenu.Items.Add(resetItem);

            // Open in browser
            var openBrowserItem = new ToolStripMenuItem("在浏览器中打开当前对话", null, (s, e) =>
            {
                OpenCurrentUrlInExternalBrowser();
            });
            contextMenu.Items.Add(openBrowserItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Exit
            var exitItem = new ToolStripMenuItem("退出 DeskSeek", null, (s, e) =>
            {
                ExitApp();
            });
            contextMenu.Items.Add(exitItem);

            contextMenu.Opening += (s, e) =>
            {
                string hk = _settingsService?.Current.Hotkey ?? "Alt+D";
                if (string.IsNullOrWhiteSpace(hk) || hk == "无" || hk.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    toggleItem.Text = "显示 / 隐藏";
                }
                else
                {
                    toggleItem.Text = $"显示 / 隐藏 ({hk})";
                }

                autoStartItem.Checked = AutoStartService.IsAutoStartEnabled();
                var currentMode = _drawerWindow?.PinMode ?? _settingsService?.Current.PinMode ?? DrawerPinMode.AutoHide;
                pinTopmostItem.Checked = (currentMode == DrawerPinMode.PinnedTopmost);
                pinNormalItem.Checked = (currentMode == DrawerPinMode.PinnedNormal);
                pinAutoHideItem.Checked = (currentMode == DrawerPinMode.AutoHide);
            };

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
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string icoPath = Path.Combine(baseDir, "Assets", "app.ico");
                if (File.Exists(icoPath))
                {
                    return new Icon(icoPath, SystemInformation.SmallIconSize);
                }

                string pngPath = Path.Combine(baseDir, "Assets", "logo.png");
                if (File.Exists(pngPath))
                {
                    using var src = System.Drawing.Image.FromFile(pngPath);
                    int size = Math.Max(16, SystemInformation.SmallIconSize.Width);
                    using var bmp = new Bitmap(size, size);
                    using var g = Graphics.FromImage(bmp);
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);

                    // Crop tightly around mascot (bounds 120,130,800,800) for crystal clarity in small tray
                    g.DrawImage(src, new Rectangle(0, 0, size, size), 120, 130, 800, 800, GraphicsUnit.Pixel);
                    return Icon.FromHandle(bmp.GetHicon());
                }

                return SystemIcons.Application;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeskSeek] Failed to load custom tray icon: {ex.Message}");
                return SystemIcons.Application;
            }
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

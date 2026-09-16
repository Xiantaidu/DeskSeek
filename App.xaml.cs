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

        public void ReloadHotkey(string newHotkey)
        {
            if (_hotkeyService != null && _ballWindow != null)
            {
                _hotkeyService.RegisterByString(_ballWindow, newHotkey);
            }
        }

        private void InitTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Text = "DeskSeek - DeepSeek 桌面轻量吸附窗 (Alt+D)",
                Visible = true,
                Icon = GenerateAppIcon()
            };

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

            // Pin state toggle
            var pinItem = new ToolStripMenuItem("固定常驻 (不自动隐藏)")
            {
                Checked = _settingsService?.Current.IsPinned ?? false,
                CheckOnClick = true
            };
            pinItem.Click += (s, e) =>
            {
                if (_settingsService != null)
                {
                    _settingsService.Current.IsPinned = pinItem.Checked;
                    _settingsService.Save();
                }
            };
            contextMenu.Items.Add(pinItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Reset ball position
            var resetItem = new ToolStripMenuItem("重置悬浮球位置", null, (s, e) =>
            {
                if (_ballWindow != null && _settingsService != null)
                {
                    _settingsService.Current.BallTop = 200;
                    _settingsService.Current.IsDockedToRight = true;
                    _ballWindow.ExpandFromEdge(instant: true);
                }
            });
            contextMenu.Items.Add(resetItem);

            // Open in browser
            var openBrowserItem = new ToolStripMenuItem("在浏览器中打开 DeepSeek", null, (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("https://chat.deepseek.com") { UseShellExecute = true });
                }
                catch { }
            });
            contextMenu.Items.Add(openBrowserItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Exit
            var exitItem = new ToolStripMenuItem("退出 DeskSeek", null, (s, e) =>
            {
                ExitApp();
            });
            contextMenu.Items.Add(exitItem);

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
                using var bmp = new Bitmap(32, 32);
                using var g = Graphics.FromImage(bmp);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // DeepSeek Blue rounded background
                using var brush = new SolidBrush(Color.FromArgb(0, 82, 217));
                g.FillEllipse(brush, 1, 1, 30, 30);

                // Stylized "D" glyph
                using var pen = new Pen(Color.White, 3.5f);
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                // Draw sleek D-shape
                g.DrawLine(pen, 10, 8, 10, 24);
                g.DrawArc(pen, 8, 8, 14, 16, -90, 180);

                var hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private void ExitApp()
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

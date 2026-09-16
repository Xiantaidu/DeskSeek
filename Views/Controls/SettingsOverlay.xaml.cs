using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DeskSeek.Services;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using Cursors = System.Windows.Input.Cursors;

namespace DeskSeek.Views.Controls
{
    public partial class SettingsOverlay : UserControl
    {
        private SettingsService? _settingsService;
        private bool _isRecordingHotkey;

        public event Action? CloseRequested;
        public event Action? ResetSizeRequested;

        public SettingsOverlay()
        {
            InitializeComponent();
        }

        public void Initialize(SettingsService settingsService)
        {
            _settingsService = settingsService;
            ReloadSettings();
        }

        public void ReloadSettings()
        {
            if (_settingsService == null) return;

            SettingAutoStart.IsChecked = AutoStartService.IsAutoStartEnabled();
            SettingAutoCollapse.IsChecked = _settingsService.Current.AutoCollapse;
            SettingSuspend.IsChecked = _settingsService.Current.SuspendWhenHidden;
            SettingRememberX.IsChecked = _settingsService.Current.RememberX;
            SettingRememberY.IsChecked = _settingsService.Current.RememberY;

            UpdateHotkeyDisplay(_settingsService.Current.Hotkey);
            StopRecordingHotkey();
            HotkeyStatusTip.Visibility = Visibility.Collapsed;
        }

        private void BackFromSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke();
        }

        private void SettingAutoStart_Click(object sender, RoutedEventArgs e)
        {
            bool val = SettingAutoStart.IsChecked == true;
            AutoStartService.SetAutoStart(val);
            if (_settingsService != null)
            {
                _settingsService.Current.AutoStart = val;
                _settingsService.Save();
            }
        }

        private void SettingAutoCollapse_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsService != null)
            {
                _settingsService.Current.AutoCollapse = SettingAutoCollapse.IsChecked == true;
                _settingsService.Save();
            }
        }

        private void SettingRememberX_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsService != null)
            {
                _settingsService.Current.RememberX = SettingRememberX.IsChecked == true;
                _settingsService.Save();
            }
        }

        private void SettingRememberY_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsService != null)
            {
                _settingsService.Current.RememberY = SettingRememberY.IsChecked == true;
                _settingsService.Save();
            }
        }

        private void SettingSuspend_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsService != null)
            {
                _settingsService.Current.SuspendWhenHidden = SettingSuspend.IsChecked == true;
                _settingsService.Save();
            }
        }

        private void ResetSizeButton_Click(object sender, RoutedEventArgs e)
        {
            ResetSizeRequested?.Invoke();
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

        #region Hotkey Recording

        private void UpdateHotkeyDisplay(string hotkey)
        {
            if (string.IsNullOrWhiteSpace(hotkey) || hotkey == "无" || hotkey.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                HotkeyDisplayText.Text = "无 (已禁用)";
                HotkeyDisplayText.Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                HotkeyRecordIcon.Text = "🚫";
            }
            else
            {
                HotkeyDisplayText.Text = hotkey;
                HotkeyDisplayText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x52, 0xD9));
                HotkeyRecordIcon.Text = "⌨️";
            }
        }

        private void HotkeyRecordBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartRecordingHotkey();
            e.Handled = true;
        }

        private void StartRecordingHotkey()
        {
            _isRecordingHotkey = true;
            HotkeyDisplayText.Text = "请在键盘直接按下快捷键...";
            HotkeyDisplayText.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x52, 0xD9));
            HotkeyRecordIcon.Text = "🔴";
            HotkeyRecordBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x52, 0xD9));
            HotkeyRecordBorder.Background = new SolidColorBrush(Color.FromArgb(0x15, 0x00, 0x52, 0xD9));
            HotkeyRecordBorder.Focus();
        }

        public void StopRecordingHotkey()
        {
            _isRecordingHotkey = false;
            HotkeyRecordBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));
            HotkeyRecordBorder.Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            if (_settingsService != null)
            {
                UpdateHotkeyDisplay(_settingsService.Current.Hotkey);
            }
        }

        private void HotkeyRecordBorder_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isRecordingHotkey)
            {
                StopRecordingHotkey();
            }
        }

        private void HotkeyRecordBorder_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_isRecordingHotkey) return;

            e.Handled = true;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Esc cancels recording
            if (key == Key.Escape)
            {
                StopRecordingHotkey();
                return;
            }

            // Backspace / Delete sets to None
            if (key == Key.Back || key == Key.Delete)
            {
                ApplyHotkey("无");
                StopRecordingHotkey();
                return;
            }

            // Ignore pure modifier presses
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            var mods = new List<string>();
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) mods.Add("Ctrl");
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) mods.Add("Alt");
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) mods.Add("Shift");
            if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) mods.Add("Win");

            string keyName = FormatKeyName(key);
            if (string.IsNullOrEmpty(keyName))
            {
                return;
            }

            string fullHotkey = mods.Count > 0
                ? string.Join("+", mods) + "+" + keyName
                : keyName;

            ApplyHotkey(fullHotkey);
            StopRecordingHotkey();
        }

        private static string FormatKeyName(Key key)
        {
            if (key >= Key.A && key <= Key.Z)
                return key.ToString();
            if (key >= Key.D0 && key <= Key.D9)
                return key.ToString().Substring(1);
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return "Num" + key.ToString().Substring(6);
            if (key >= Key.F1 && key <= Key.F24)
                return key.ToString();
            if (key == Key.Space)
                return "Space";
            if (key == Key.Tab)
                return "Tab";
            if (key == Key.Enter || key == Key.Return)
                return "Enter";
            if (key == Key.OemTilde)
                return "`";
            if (key == Key.OemMinus)
                return "-";
            if (key == Key.OemPlus)
                return "=";
            if (key == Key.OemQuestion)
                return "/";

            return key.ToString();
        }

        private void ClearHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyHotkey("无");
        }

        private void PresetChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement btn && btn.Tag is string preset)
            {
                ApplyHotkey(preset);
            }
        }

        private void ApplyHotkey(string hotkey)
        {
            if (_settingsService != null)
            {
                _settingsService.Current.Hotkey = hotkey;
                _settingsService.Save();
            }

            bool success = App.Instance?.ReloadHotkey(hotkey) ?? true;
            UpdateHotkeyDisplay(hotkey);

            if (!success && hotkey != "无")
            {
                HotkeyStatusTip.Text = "⚠️ 热键注册失败：可能已被系统或其他应用占用";
                HotkeyStatusTip.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                HotkeyStatusTip.Visibility = Visibility.Visible;
            }
            else
            {
                HotkeyStatusTip.Text = hotkey == "无" ? "已禁用快捷键唤醒" : $"✓ 热键已生效：{hotkey}";
                HotkeyStatusTip.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
                HotkeyStatusTip.Visibility = Visibility.Visible;
            }
        }

        #endregion
    }
}

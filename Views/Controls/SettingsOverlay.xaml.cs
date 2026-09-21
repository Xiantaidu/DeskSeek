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
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
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

            ThemeService.Instance.ThemeChanged += (isDark, accent) =>
            {
                UpdateLanguageUi();
                UpdateThemeUi();
                if (_settingsService != null)
                {
                    UpdateHotkeyDisplay(_settingsService.Current.Hotkey);
                }
            };
        }

        public void Initialize(SettingsService settingsService)
        {
            _settingsService = settingsService;
            ReloadSettings();
        }

        public void ReloadSettings()
        {
            if (_settingsService == null) return;

            UpdateLanguageUi();
            UpdateThemeUi();

            SettingAutoStart.IsChecked = AutoStartService.IsAutoStartEnabled();
            SettingAutoCollapse.IsChecked = _settingsService.Current.AutoCollapse;
            SettingSuspend.IsChecked = _settingsService.Current.SuspendWhenHidden;
            SettingRememberX.IsChecked = _settingsService.Current.RememberX;
            SettingRememberY.IsChecked = _settingsService.Current.RememberY;

            UpdateHotkeyDisplay(_settingsService.Current.Hotkey);
            StopRecordingHotkey();
            HotkeyStatusTip.Visibility = Visibility.Collapsed;
        }

        private void LangZhButton_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage(LocalizationService.Chinese);
        }

        private void LangEnButton_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage(LocalizationService.English);
        }

        private void SetLanguage(string lang)
        {
            LocalizationService.Instance.SetLanguage(lang);
            if (_settingsService != null)
            {
                _settingsService.Current.Language = lang;
                _settingsService.Save();
            }
            UpdateLanguageUi();
            if (_settingsService != null)
            {
                UpdateHotkeyDisplay(_settingsService.Current.Hotkey);
            }
        }

        private void UpdateLanguageUi()
        {
            bool isZh = LocalizationService.Instance.CurrentLanguage != LocalizationService.English;
            var accentBrush = Application.Current?.Resources["Theme_AccentBrush"] as Brush ?? new SolidColorBrush(ThemeService.Instance.CurrentAccentColor);
            var textSecondary = Application.Current?.Resources["Theme_TextSecondary"] as Brush ?? new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));

            LangZhButton.Background = isZh ? accentBrush : Brushes.Transparent;
            LangZhButton.Foreground = isZh ? Brushes.White : textSecondary;

            LangEnButton.Background = !isZh ? accentBrush : Brushes.Transparent;
            LangEnButton.Foreground = !isZh ? Brushes.White : textSecondary;
        }

        #region Theme & Accent Handlers

        private void ThemeAutoButton_Click(object sender, RoutedEventArgs e)
        {
            SetThemeMode(ThemeService.ModeSystem);
        }

        private void ThemeLightButton_Click(object sender, RoutedEventArgs e)
        {
            SetThemeMode(ThemeService.ModeLight);
        }

        private void ThemeDarkButton_Click(object sender, RoutedEventArgs e)
        {
            SetThemeMode(ThemeService.ModeDark);
        }

        private void SetThemeMode(string mode)
        {
            ThemeService.Instance.SetThemeMode(mode);
            if (_settingsService != null)
            {
                _settingsService.Current.ThemeMode = mode;
                _settingsService.Save();
            }
            UpdateThemeUi();
        }

        private void AccentSystemButton_Click(object sender, RoutedEventArgs e)
        {
            SetAccentMode(ThemeService.AccentSystem);
        }

        private void AccentDeepSeekButton_Click(object sender, RoutedEventArgs e)
        {
            SetAccentMode(ThemeService.AccentDeepSeek);
        }

        private void SetAccentMode(string mode)
        {
            ThemeService.Instance.SetAccentMode(mode);
            if (_settingsService != null)
            {
                _settingsService.Current.AccentMode = mode;
                _settingsService.Save();
            }
            UpdateThemeUi();
        }

        private void UpdateThemeUi()
        {
            var accentBrush = Application.Current?.Resources["Theme_AccentBrush"] as Brush ?? new SolidColorBrush(ThemeService.Instance.CurrentAccentColor);
            var textSecondary = Application.Current?.Resources["Theme_TextSecondary"] as Brush ?? new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));

            // Windows Accent preview dot
            WindowsAccentPreviewDot.Fill = new SolidColorBrush(ThemeService.GetWindowsAccentColor());

            // Color Mode buttons
            string themeMode = _settingsService?.Current.ThemeMode ?? ThemeService.Instance.CurrentThemeMode;
            bool isAuto = themeMode == ThemeService.ModeSystem;
            bool isLight = themeMode == ThemeService.ModeLight;
            bool isDark = themeMode == ThemeService.ModeDark;

            ThemeAutoButton.Background = isAuto ? accentBrush : Brushes.Transparent;
            ThemeAutoButton.Foreground = isAuto ? Brushes.White : textSecondary;

            ThemeLightButton.Background = isLight ? accentBrush : Brushes.Transparent;
            ThemeLightButton.Foreground = isLight ? Brushes.White : textSecondary;

            ThemeDarkButton.Background = isDark ? accentBrush : Brushes.Transparent;
            ThemeDarkButton.Foreground = isDark ? Brushes.White : textSecondary;

            // Accent Color buttons
            string accentMode = _settingsService?.Current.AccentMode ?? ThemeService.Instance.CurrentAccentMode;
            bool isSystemAccent = accentMode != ThemeService.AccentDeepSeek;

            AccentSystemButton.Background = isSystemAccent ? accentBrush : Brushes.Transparent;
            AccentSystemButton.Foreground = isSystemAccent ? Brushes.White : textSecondary;

            AccentDeepSeekButton.Background = !isSystemAccent ? accentBrush : Brushes.Transparent;
            AccentDeepSeekButton.Foreground = !isSystemAccent ? Brushes.White : textSecondary;
        }

        #endregion

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
            var textMuted = Application.Current?.Resources["Theme_TextMuted"] as Brush ?? new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
            var accentBrush = Application.Current?.Resources["Theme_AccentBrush"] as Brush ?? new SolidColorBrush(ThemeService.Instance.CurrentAccentColor);

            if (string.IsNullOrWhiteSpace(hotkey) || hotkey == "无" || hotkey.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                HotkeyDisplayText.Text = LocalizationService.Instance.GetString("Lang_HotkeyNoneText");
                HotkeyDisplayText.Foreground = textMuted;
                HotkeyRecordIcon.Text = "🚫";
            }
            else
            {
                HotkeyDisplayText.Text = hotkey;
                HotkeyDisplayText.Foreground = accentBrush;
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
            var accentBrush = Application.Current?.Resources["Theme_AccentBrush"] as Brush ?? new SolidColorBrush(ThemeService.Instance.CurrentAccentColor);
            _isRecordingHotkey = true;
            HotkeyDisplayText.Text = LocalizationService.Instance.GetString("Lang_HotkeyRecordingText");
            HotkeyDisplayText.Foreground = accentBrush;
            HotkeyRecordIcon.Text = "🔴";
            HotkeyRecordBorder.BorderBrush = accentBrush;
            HotkeyRecordBorder.Background = new SolidColorBrush(Color.FromArgb(0x18, ThemeService.Instance.CurrentAccentColor.R, ThemeService.Instance.CurrentAccentColor.G, ThemeService.Instance.CurrentAccentColor.B));
            HotkeyRecordBorder.Focus();
        }

        public void StopRecordingHotkey()
        {
            _isRecordingHotkey = false;
            var cardBorder = Application.Current?.Resources["Theme_CardInnerBorder"] as Brush ?? new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));
            var segmentBg = Application.Current?.Resources["Theme_SegmentBackground"] as Brush ?? new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            HotkeyRecordBorder.BorderBrush = cardBorder;
            HotkeyRecordBorder.Background = segmentBg;
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

            bool isNone = string.IsNullOrWhiteSpace(hotkey) || hotkey == "无" || hotkey.Equals("None", StringComparison.OrdinalIgnoreCase);

            if (!success && !isNone)
            {
                HotkeyStatusTip.Text = LocalizationService.Instance.GetString("Lang_HotkeyFailedMsg");
                HotkeyStatusTip.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                HotkeyStatusTip.Visibility = Visibility.Visible;
            }
            else
            {
                HotkeyStatusTip.Text = isNone
                    ? LocalizationService.Instance.GetString("Lang_HotkeyDisabledMsg")
                    : LocalizationService.Instance.GetString("Lang_HotkeySuccessPrefix") + hotkey;
                HotkeyStatusTip.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
                HotkeyStatusTip.Visibility = Visibility.Visible;
            }
        }

        #endregion
    }
}

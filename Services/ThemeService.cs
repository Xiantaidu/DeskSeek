using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using Application = System.Windows.Application;
using SystemColors = System.Windows.SystemColors;

namespace DeskSeek.Services
{
    public class ThemeService
    {
        private static ThemeService? _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        public const string ModeSystem = "System";
        public const string ModeLight = "Light";
        public const string ModeDark = "Dark";

        public const string AccentSystem = "System";
        public const string AccentDeepSeek = "DeepSeekBlue";

        public static readonly Color DeepSeekBlue = Color.FromRgb(0x00, 0x52, 0xD9);

        public string CurrentThemeMode { get; private set; } = ModeSystem;
        public string CurrentAccentMode { get; private set; } = AccentSystem;
        public bool IsDark { get; private set; }
        public Color CurrentAccentColor { get; private set; } = DeepSeekBlue;

        public event Action<bool, Color>? ThemeChanged;

        [DllImport("dwmapi.dll", EntryPoint = "#127", PreserveSig = false)]
        private static extern void DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);

        public ThemeService()
        {
            try
            {
                SystemEvents.UserPreferenceChanged += (s, e) =>
                {
                    if (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.Color)
                    {
                        Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ApplyTheme(triggerEvent: true);
                        }));
                    }
                };
            }
            catch { }
        }

        public void Initialize(string themeMode, string accentMode)
        {
            CurrentThemeMode = string.IsNullOrWhiteSpace(themeMode) ? ModeSystem : themeMode;
            CurrentAccentMode = string.IsNullOrWhiteSpace(accentMode) ? AccentSystem : accentMode;
            ApplyTheme(triggerEvent: true);
        }

        public void SetThemeMode(string themeMode)
        {
            if (CurrentThemeMode == themeMode) return;
            CurrentThemeMode = themeMode;
            ApplyTheme(triggerEvent: true);
        }

        public void SetAccentMode(string accentMode)
        {
            if (CurrentAccentMode == accentMode) return;
            CurrentAccentMode = accentMode;
            ApplyTheme(triggerEvent: true);
        }

        public void OnWindowsSettingChanged()
        {
            ApplyTheme(triggerEvent: true);
        }

        public static bool IsWindowsDarkTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var val = key?.GetValue("AppsUseLightTheme");
                if (val is int intVal)
                {
                    return intVal == 0;
                }
            }
            catch { }
            return false;
        }

        public static Color GetWindowsAccentColor()
        {
            // Primary path: .NET 9 native SystemColors.AccentColor
            try
            {
                var accent = SystemColors.AccentColor;
                if (accent.A > 0 && !(accent.R == 0 && accent.G == 0 && accent.B == 0))
                {
                    return accent;
                }
            }
            catch { }

            // Secondary path: DWM ColorizationColor
            try
            {
                DwmGetColorizationColor(out uint colorization, out _);
                byte a = (byte)((colorization >> 24) & 0xFF);
                byte r = (byte)((colorization >> 16) & 0xFF);
                byte g = (byte)((colorization >> 8) & 0xFF);
                byte b = (byte)(colorization & 0xFF);
                if (r > 0 || g > 0 || b > 0)
                {
                    return Color.FromArgb(255, r, g, b);
                }
            }
            catch { }

            // Fallback: DeepSeek brand blue
            return DeepSeekBlue;
        }

        private void ApplyTheme(bool triggerEvent)
        {
            // 1. Determine effective IsDark
            IsDark = CurrentThemeMode switch
            {
                ModeLight => false,
                ModeDark => true,
                _ => IsWindowsDarkTheme()
            };

            // 2. Determine effective AccentColor
            CurrentAccentColor = CurrentAccentMode switch
            {
                AccentDeepSeek => DeepSeekBlue,
                _ => GetWindowsAccentColor()
            };

            var appRes = Application.Current?.Resources;
            if (appRes != null)
            {
                // Accent Colors & Brushes
                var accentBrush = CreateFrozenBrush(CurrentAccentColor);
                var accentHoverBrush = CreateFrozenBrush(AdjustBrightness(CurrentAccentColor, 1.15f));
                var accentPressedBrush = CreateFrozenBrush(AdjustBrightness(CurrentAccentColor, 0.85f));
                var accentGlow = Color.FromArgb(140, CurrentAccentColor.R, CurrentAccentColor.G, CurrentAccentColor.B);

                appRes["Theme_AccentColor"] = CurrentAccentColor;
                appRes["Theme_AccentGlowColor"] = accentGlow;
                appRes["Theme_AccentBrush"] = accentBrush;
                appRes["Theme_AccentHoverBrush"] = accentHoverBrush;
                appRes["Theme_AccentPressedBrush"] = accentPressedBrush;

                if (IsDark)
                {
                    // Dark Palette
                    appRes["Theme_CardBackground"] = CreateFrozenBrush(Color.FromRgb(0x1E, 0x1F, 0x24));
                    appRes["Theme_CardBorder"] = CreateFrozenBrush(Color.FromRgb(0x2E, 0x32, 0x3B));
                    appRes["Theme_HeaderBackground"] = CreateFrozenBrush(Color.FromRgb(0x18, 0x19, 0x1E));
                    appRes["Theme_HeaderBorder"] = CreateFrozenBrush(Color.FromRgb(0x28, 0x2B, 0x33));
                    appRes["Theme_FooterBackground"] = CreateFrozenBrush(Color.FromRgb(0x1E, 0x1F, 0x24));
                    appRes["Theme_OverlayBackground"] = CreateFrozenBrush(Color.FromRgb(0x14, 0x15, 0x18));
                    appRes["Theme_CardInnerBackground"] = CreateFrozenBrush(Color.FromRgb(0x1E, 0x1F, 0x24));
                    appRes["Theme_CardInnerBorder"] = CreateFrozenBrush(Color.FromRgb(0x2E, 0x32, 0x3B));

                    appRes["Theme_TextPrimary"] = CreateFrozenBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
                    appRes["Theme_TextSecondary"] = CreateFrozenBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                    appRes["Theme_TextMuted"] = CreateFrozenBrush(Color.FromRgb(0x64, 0x74, 0x8B));

                    appRes["Theme_ButtonHover"] = CreateFrozenBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF));
                    appRes["Theme_ButtonPressed"] = CreateFrozenBrush(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF));
                    appRes["Theme_ResizeBar"] = CreateFrozenBrush(Color.FromRgb(0x47, 0x55, 0x69));

                    appRes["Theme_SegmentBackground"] = CreateFrozenBrush(Color.FromRgb(0x26, 0x29, 0x30));
                    appRes["Theme_SegmentHover"] = CreateFrozenBrush(Color.FromRgb(0x33, 0x37, 0x42));

                    appRes["Theme_ContextMenuBackground"] = CreateFrozenBrush(Color.FromRgb(0x1E, 0x1F, 0x24));
                    appRes["Theme_ContextMenuBorder"] = CreateFrozenBrush(Color.FromRgb(0x2D, 0x31, 0x39));
                    appRes["Theme_ContextMenuHover"] = CreateFrozenBrush(Color.FromRgb(0x2A, 0x2D, 0x36));
                    appRes["Theme_ContextMenuText"] = CreateFrozenBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
                    appRes["Theme_ContextMenuSeparator"] = CreateFrozenBrush(Color.FromRgb(0x2E, 0x32, 0x3B));

                    appRes["Theme_DisclaimerBoxBackground"] = CreateFrozenBrush(Color.FromRgb(0x18, 0x19, 0x1E));
                    appRes["Theme_DisclaimerBoxBorder"] = CreateFrozenBrush(Color.FromRgb(0x28, 0x2B, 0x33));
                }
                else
                {
                    // Light Palette
                    appRes["Theme_CardBackground"] = CreateFrozenBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                    appRes["Theme_CardBorder"] = CreateFrozenBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));
                    appRes["Theme_HeaderBackground"] = CreateFrozenBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
                    appRes["Theme_HeaderBorder"] = CreateFrozenBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
                    appRes["Theme_FooterBackground"] = CreateFrozenBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                    appRes["Theme_OverlayBackground"] = CreateFrozenBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
                    appRes["Theme_CardInnerBackground"] = CreateFrozenBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                    appRes["Theme_CardInnerBorder"] = CreateFrozenBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));

                    appRes["Theme_TextPrimary"] = CreateFrozenBrush(Color.FromRgb(0x0F, 0x17, 0x2A));
                    appRes["Theme_TextSecondary"] = CreateFrozenBrush(Color.FromRgb(0x64, 0x74, 0x8B));
                    appRes["Theme_TextMuted"] = CreateFrozenBrush(Color.FromRgb(0x94, 0xA3, 0xB8));

                    appRes["Theme_ButtonHover"] = CreateFrozenBrush(Color.FromArgb(0x15, 0x00, 0x00, 0x00));
                    appRes["Theme_ButtonPressed"] = CreateFrozenBrush(Color.FromArgb(0x25, 0x00, 0x00, 0x00));
                    appRes["Theme_ResizeBar"] = CreateFrozenBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));

                    appRes["Theme_SegmentBackground"] = CreateFrozenBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
                    appRes["Theme_SegmentHover"] = CreateFrozenBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));

                    appRes["Theme_ContextMenuBackground"] = CreateFrozenBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                    appRes["Theme_ContextMenuBorder"] = CreateFrozenBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
                    appRes["Theme_ContextMenuHover"] = CreateFrozenBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
                    appRes["Theme_ContextMenuText"] = CreateFrozenBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
                    appRes["Theme_ContextMenuSeparator"] = CreateFrozenBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));

                    appRes["Theme_DisclaimerBoxBackground"] = CreateFrozenBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
                    appRes["Theme_DisclaimerBoxBorder"] = CreateFrozenBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
                }
            }

            if (triggerEvent)
            {
                ThemeChanged?.Invoke(IsDark, CurrentAccentColor);
            }
        }

        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static Color AdjustBrightness(Color color, float factor)
        {
            float r = Math.Clamp(color.R * factor, 0, 255);
            float g = Math.Clamp(color.G * factor, 0, 255);
            float b = Math.Clamp(color.B * factor, 0, 255);
            return Color.FromArgb(color.A, (byte)r, (byte)g, (byte)b);
        }
    }
}

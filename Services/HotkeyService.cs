using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DeskSeek.Services
{
    public class HotkeyService : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9001;

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private HwndSource? _source;
        private IntPtr _hWnd;
        private bool _isRegistered;

        public event Action? HotkeyPressed;
        public bool IsRegistered => _isRegistered;
        public string CurrentHotkey { get; private set; } = "Alt+D";

        public bool Register(Window window, uint modifiers = MOD_ALT, uint vk = 0x44 /* D */)
        {
            Unregister();

            var helper = new WindowInteropHelper(window);
            _hWnd = helper.EnsureHandle();

            _source = HwndSource.FromHwnd(_hWnd);
            if (_source != null)
            {
                _source.AddHook(HwndHook);
            }

            _isRegistered = RegisterHotKey(_hWnd, HOTKEY_ID, modifiers | MOD_NOREPEAT, vk);
            return _isRegistered;
        }

        public bool RegisterByString(Window window, string hotkey)
        {
            CurrentHotkey = hotkey;

            if (string.IsNullOrWhiteSpace(hotkey) ||
                string.Equals(hotkey, "无", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(hotkey, "None", StringComparison.OrdinalIgnoreCase))
            {
                Unregister();
                return true; // Successfully unregistered (disabled)
            }

            if (!TryParseHotkey(hotkey, out uint mod, out uint vk))
            {
                Unregister();
                return false;
            }

            return Register(window, mod, vk);
        }

        public static bool TryParseHotkey(string hotkey, out uint modifiers, out uint vk)
        {
            modifiers = 0;
            vk = 0;

            if (string.IsNullOrWhiteSpace(hotkey) ||
                string.Equals(hotkey, "无", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(hotkey, "None", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string[] parts = hotkey.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string mainKeyStr = "";

            foreach (var part in parts)
            {
                var p = part.Trim();
                if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || p.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    modifiers |= MOD_CONTROL;
                else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    modifiers |= MOD_ALT;
                else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    modifiers |= MOD_SHIFT;
                else if (p.Equals("Win", StringComparison.OrdinalIgnoreCase) || p.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                    modifiers |= MOD_WIN;
                else
                    mainKeyStr = p;
            }

            if (string.IsNullOrEmpty(mainKeyStr))
                return false;

            vk = ParseVirtualKey(mainKeyStr);
            return vk != 0;
        }

        private static uint ParseVirtualKey(string keyStr)
        {
            // Common aliases
            if (keyStr.Equals("Space", StringComparison.OrdinalIgnoreCase) || keyStr.Equals("空格", StringComparison.OrdinalIgnoreCase))
                return 0x20;
            if (keyStr.Equals("Esc", StringComparison.OrdinalIgnoreCase) || keyStr.Equals("Escape", StringComparison.OrdinalIgnoreCase))
                return 0x1B;
            if (keyStr.Equals("Tab", StringComparison.OrdinalIgnoreCase))
                return 0x09;
            if (keyStr.Equals("Enter", StringComparison.OrdinalIgnoreCase) || keyStr.Equals("Return", StringComparison.OrdinalIgnoreCase))
                return 0x0D;
            if (keyStr.Equals("Back", StringComparison.OrdinalIgnoreCase) || keyStr.Equals("Backspace", StringComparison.OrdinalIgnoreCase))
                return 0x08;
            if (keyStr.Equals("`", StringComparison.OrdinalIgnoreCase) || keyStr.Equals("~", StringComparison.OrdinalIgnoreCase))
                return 0xC0; // VK_OEM_3
            if (keyStr.Equals("-", StringComparison.OrdinalIgnoreCase))
                return 0xBD; // VK_OEM_MINUS
            if (keyStr.Equals("=", StringComparison.OrdinalIgnoreCase))
                return 0xBB; // VK_OEM_PLUS
            if (keyStr.Equals("/", StringComparison.OrdinalIgnoreCase))
                return 0xBF; // VK_OEM_2

            if (keyStr.Length == 1 && char.IsDigit(keyStr[0]))
                return (uint)keyStr[0]; // '0'-'9' (0x30-0x39)

            if (keyStr.Length == 1 && char.IsLetter(keyStr[0]))
                return (uint)char.ToUpperInvariant(keyStr[0]); // 'A'-'Z' (0x41-0x5A)

            if (keyStr.StartsWith("Num", StringComparison.OrdinalIgnoreCase) &&
                keyStr.Length > 3 && char.IsDigit(keyStr[3]))
            {
                return (uint)(0x60 + (keyStr[3] - '0')); // VK_NUMPAD0 = 0x60
            }

            if (keyStr.StartsWith("F", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(keyStr.Substring(1), out int fNum) && fNum >= 1 && fNum <= 24)
            {
                return (uint)(0x70 + (fNum - 1)); // VK_F1 = 0x70
            }

            if (Enum.TryParse<Key>(keyStr, true, out var key))
            {
                int k = KeyInterop.VirtualKeyFromKey(key);
                if (k > 0) return (uint)k;
            }

            return 0;
        }

        public void Unregister()
        {
            if (_isRegistered && _hWnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hWnd, HOTKEY_ID);
                _isRegistered = false;
            }

            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source = null;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unregister();
            GC.SuppressFinalize(this);
        }
    }
}

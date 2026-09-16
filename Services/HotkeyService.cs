using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DeskSeek.Services
{
    public class HotkeyService : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9001;

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private HwndSource? _source;
        private IntPtr _hWnd;
        private bool _isRegistered;

        public event Action? HotkeyPressed;

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
            uint mod = 0;
            uint vk = 0x44; // D

            if (hotkey.Contains("Alt", StringComparison.OrdinalIgnoreCase)) mod |= MOD_ALT;
            if (hotkey.Contains("Ctrl", StringComparison.OrdinalIgnoreCase)) mod |= MOD_CONTROL;
            if (hotkey.Contains("Shift", StringComparison.OrdinalIgnoreCase)) mod |= MOD_SHIFT;
            if (hotkey.Contains("Win", StringComparison.OrdinalIgnoreCase)) mod |= MOD_WIN;

            if (hotkey.EndsWith("Space", StringComparison.OrdinalIgnoreCase)) vk = 0x20;
            else if (hotkey.EndsWith("D", StringComparison.OrdinalIgnoreCase)) vk = 0x44;
            else if (hotkey.EndsWith("S", StringComparison.OrdinalIgnoreCase)) vk = 0x53;

            return Register(window, mod, vk);
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

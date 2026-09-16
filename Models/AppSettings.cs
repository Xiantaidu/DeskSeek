namespace DeskSeek.Models
{
    public enum DrawerPinMode
    {
        AutoHide = 0,       // 失焦自动收起
        PinnedTopmost = 1,  // 常驻且置顶
        PinnedNormal = 2    // 常驻但不置顶
    }

    public class AppSettings
    {
        public double BallTop { get; set; } = 200;
        public double BallLeft { get; set; } = -1; // -1 indicates initial default right-edge dock
        public bool IsDockedToRight { get; set; } = true;
        public bool IsPinned { get; set; } = false;
        public DrawerPinMode PinMode { get; set; } = DrawerPinMode.AutoHide;
        public bool AutoStart { get; set; } = false;
        public double DrawerWidth { get; set; } = 460;
        public double DrawerHeight { get; set; } = -1;
        public double WebZoom { get; set; } = 1.0;
        public string Hotkey { get; set; } = "Alt+D";
        public bool AutoCollapse { get; set; } = true;
        public bool SuspendWhenHidden { get; set; } = false;
        public string Language { get; set; } = "zh-CN";
        public bool DisclaimerAccepted { get; set; } = false;
        public bool RememberX { get; set; } = true;
        public bool RememberY { get; set; } = false;
        public double LastDrawerX { get; set; } = -1;
        public double LastDrawerY { get; set; } = -1;
    }
}

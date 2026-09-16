namespace DeskSeek.Models
{
    public class AppSettings
    {
        public double BallTop { get; set; } = 200;
        public double BallLeft { get; set; } = -1; // -1 indicates initial default right-edge dock
        public bool IsDockedToRight { get; set; } = true;
        public bool IsPinned { get; set; } = false;
        public bool AutoStart { get; set; } = false;
        public double DrawerWidth { get; set; } = 460;
        public double DrawerHeight { get; set; } = -1;
        public double WebZoom { get; set; } = 1.0;
        public string Hotkey { get; set; } = "Alt+D";
        public bool AutoCollapse { get; set; } = true;
        public bool SuspendWhenHidden { get; set; } = false;
        public bool DisclaimerAccepted { get; set; } = false;
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DeskSeek.Services;
using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;

namespace DeskSeek.Views.Controls
{
    public partial class DisclaimerOverlay : UserControl
    {
        private readonly SettingsService? _settingsService;
        private DispatcherTimer? _disclaimerTimer;
        private int _countdown = 3;

        public event Action? Accepted;

        public DisclaimerOverlay()
        {
            InitializeComponent();
        }

        public DisclaimerOverlay(SettingsService settingsService) : this()
        {
            _settingsService = settingsService;
        }

        public void StartCountdown()
        {
            if (_disclaimerTimer != null) return;

            _countdown = 3;
            DisclaimerCheckBox.IsEnabled = false;
            DisclaimerCheckBox.IsChecked = false;
            DisclaimerAgreeButton.IsEnabled = false;
            DisclaimerAgreeButton.Background = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
            DisclaimerAgreeButton.Cursor = Cursors.No;
            DisclaimerButtonText.Text = $"请先阅读免责声明 ({_countdown}s)";

            _disclaimerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _disclaimerTimer.Tick += (s, e) =>
            {
                _countdown--;
                if (_countdown > 0)
                {
                    DisclaimerButtonText.Text = $"请先阅读免责声明 ({_countdown}s)";
                }
                else
                {
                    _disclaimerTimer.Stop();
                    _disclaimerTimer = null;
                    DisclaimerCheckBox.IsEnabled = true;
                    DisclaimerButtonText.Text = "请勾选已阅读同意";
                }
            };
            _disclaimerTimer.Start();
        }

        private void DisclaimerCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = DisclaimerCheckBox.IsChecked == true;
            if (isChecked)
            {
                DisclaimerAgreeButton.IsEnabled = true;
                DisclaimerAgreeButton.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x52, 0xD9));
                DisclaimerAgreeButton.Cursor = Cursors.Hand;
                DisclaimerButtonText.Text = "同意并开始使用";
            }
            else
            {
                DisclaimerAgreeButton.IsEnabled = false;
                DisclaimerAgreeButton.Background = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                DisclaimerAgreeButton.Cursor = Cursors.No;
                DisclaimerButtonText.Text = _countdown > 0
                    ? $"请先阅读免责声明 ({_countdown}s)"
                    : "请勾选已阅读同意";
            }
        }

        private void DisclaimerAgreeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DisclaimerCheckBox.IsChecked != true) return;

            _disclaimerTimer?.Stop();
            _disclaimerTimer = null;

            if (_settingsService != null)
            {
                _settingsService.Current.DisclaimerAccepted = true;
                _settingsService.Save();
            }

            Accepted?.Invoke();
        }
    }
}

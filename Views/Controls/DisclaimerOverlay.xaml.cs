using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DeskSeek.Services;
using UserControl = System.Windows.Controls.UserControl;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using Cursors = System.Windows.Input.Cursors;

namespace DeskSeek.Views.Controls
{
    public partial class DisclaimerOverlay : UserControl
    {
        private SettingsService? _settingsService;
        private DispatcherTimer? _disclaimerTimer;
        private int _countdown = 3;

        public event Action? Accepted;

        public DisclaimerOverlay()
        {
            InitializeComponent();
            LocalizationService.Instance.LanguageChanged += _ => UpdateButtonText();
            ThemeService.Instance.ThemeChanged += (isDark, accent) => UpdateButtonColor();
        }

        public void Initialize(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public DisclaimerOverlay(SettingsService settingsService) : this()
        {
            _settingsService = settingsService;
        }

        private Brush GetAccentBrush() =>
            Application.Current?.Resources["Theme_AccentBrush"] as Brush ?? new SolidColorBrush(ThemeService.Instance.CurrentAccentColor);

        private Brush GetDisabledBrush() =>
            Application.Current?.Resources["Theme_ResizeBar"] as Brush ?? new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));

        private void UpdateButtonColor()
        {
            if (DisclaimerAgreeButton == null) return;
            if (DisclaimerCheckBox.IsChecked == true)
            {
                DisclaimerAgreeButton.Background = GetAccentBrush();
            }
            else
            {
                DisclaimerAgreeButton.Background = GetDisabledBrush();
            }
        }

        public void StartCountdown()
        {
            if (_disclaimerTimer != null) return;

            _countdown = 3;
            DisclaimerCheckBox.IsEnabled = false;
            DisclaimerCheckBox.IsChecked = false;
            DisclaimerAgreeButton.IsEnabled = false;
            DisclaimerAgreeButton.Background = GetDisabledBrush();
            DisclaimerAgreeButton.Cursor = Cursors.No;
            UpdateButtonText();

            _disclaimerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _disclaimerTimer.Tick += (s, e) =>
            {
                _countdown--;
                if (_countdown > 0)
                {
                    UpdateButtonText();
                }
                else
                {
                    _disclaimerTimer.Stop();
                    _disclaimerTimer = null;
                    DisclaimerCheckBox.IsEnabled = true;
                    UpdateButtonText();
                }
            };
            _disclaimerTimer.Start();
        }

        private void UpdateButtonText()
        {
            if (DisclaimerCheckBox.IsChecked == true)
            {
                DisclaimerButtonText.Text = LocalizationService.Instance.GetString("Lang_DisclaimerBtn");
            }
            else if (_countdown > 0)
            {
                DisclaimerButtonText.Text = string.Format(LocalizationService.Instance.GetString("Lang_DisclaimerCountdown"), _countdown);
            }
            else
            {
                DisclaimerButtonText.Text = LocalizationService.Instance.GetString("Lang_DisclaimerPleaseCheck");
            }
        }

        private void DisclaimerCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = DisclaimerCheckBox.IsChecked == true;
            if (isChecked)
            {
                DisclaimerAgreeButton.IsEnabled = true;
                DisclaimerAgreeButton.Background = GetAccentBrush();
                DisclaimerAgreeButton.Cursor = Cursors.Hand;
            }
            else
            {
                DisclaimerAgreeButton.IsEnabled = false;
                DisclaimerAgreeButton.Background = GetDisabledBrush();
                DisclaimerAgreeButton.Cursor = Cursors.No;
            }
            UpdateButtonText();
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

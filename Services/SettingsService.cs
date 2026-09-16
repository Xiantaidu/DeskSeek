using System;
using System.IO;
using System.Text.Json;
using DeskSeek.Models;

namespace DeskSeek.Services
{
    public class SettingsService
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DeskSeek");

        private static readonly string SettingsFilePath = Path.Combine(SettingsDir, "settings.json");

        public AppSettings Current { get; private set; } = new();

        public SettingsService()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null)
                    {
                        Current = loaded;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeskSeek] Failed to load settings: {ex.Message}");
            }

            Current = new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeskSeek] Failed to save settings: {ex.Message}");
            }
        }
    }
}

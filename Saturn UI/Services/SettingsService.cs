using System;
using System.IO;
using System.Text.Json;

namespace SaturnUI.Services;

public class AppSettings
{
    public string HttpBaseUrl { get; set; } = "http://127.0.0.1:8000";
    public string GrpcAddress { get; set; } = "http://127.0.0.1:50051";
    public string Protocol { get; set; } = "HTTP";
    public string Theme { get; set; } = "DeepSpace";
    public double FontSize { get; set; } = 14;
    public bool PerformanceMode { get; set; } = false;
}

public class SettingsService
{
    private readonly string _settingsPath;
    private AppSettings _settings = new();

    public AppSettings Settings => _settings;

    public event EventHandler? SettingsChanged;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "SaturnUI");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
        Load();
    }

    public void Load()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    // Validate theme key, fallback to DeepSpace if invalid
                    if (!ThemeDefinitions.ThemeKeys.Contains(loaded.Theme))
                        loaded.Theme = "DeepSpace";
                    _settings = loaded;
                }
            }
            catch { /* ignore corrupted settings */ }
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch { /* ignore save errors */ }
    }

    public void Update(Action<AppSettings> update)
    {
        update(_settings);
        Save();
    }
}

using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SaturnUI.Services;

public class AppSettings
{
    public string HttpBaseUrl { get; set; } = AppConstants.DefaultHttpBaseUrl;
    public string GrpcAddress { get; set; } = AppConstants.DefaultGrpcAddress;
    public string Protocol { get; set; } = AppConstants.ProtocolHttp;
    public string Theme { get; set; } = AppConstants.DefaultTheme;
    public double FontSize { get; set; } = AppConstants.DefaultFontSize;
    public bool PerformanceMode { get; set; }

    // ???????? / ??
    public string Provider { get; set; } = AppConstants.ProviderLocal;

    // OpenAI ?? API ??
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = AppConstants.DefaultOpenAiModel;
    public string OpenAiBaseUrl { get; set; } = AppConstants.DefaultOpenAiBaseUrl;
    public double OpenAiTemperature { get; set; } = AppConstants.DefaultTemperature;
    public int OpenAiMaxTokens { get; set; } = AppConstants.DefaultMaxTokens;

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(HttpBaseUrl)) HttpBaseUrl = AppConstants.DefaultHttpBaseUrl;
        if (string.IsNullOrWhiteSpace(GrpcAddress)) GrpcAddress = AppConstants.DefaultGrpcAddress;
        if (Protocol is not (AppConstants.ProtocolHttp or AppConstants.ProtocolGrpc)) Protocol = AppConstants.ProtocolHttp;
        if (!ThemeDefinitions.ThemeKeys.Contains(Theme)) Theme = AppConstants.DefaultTheme;
        if (FontSize < 11 || FontSize > 24) FontSize = AppConstants.DefaultFontSize;
        if (Provider is not (AppConstants.ProviderLocal or AppConstants.ProviderOnline)) Provider = AppConstants.ProviderLocal;
        if (string.IsNullOrWhiteSpace(OpenAiModel)) OpenAiModel = AppConstants.DefaultOpenAiModel;
        if (string.IsNullOrWhiteSpace(OpenAiBaseUrl)) OpenAiBaseUrl = AppConstants.DefaultOpenAiBaseUrl;
        OpenAiTemperature = Math.Clamp(OpenAiTemperature, 0, 2);
        OpenAiMaxTokens = Math.Clamp(OpenAiMaxTokens, 1, 128_000);
    }
}

public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsPath;
    private AppSettings _settings = new();

    public AppSettings Settings => _settings;
    public string SettingsPath => _settingsPath;

    public event EventHandler? SettingsChanged;

    public SettingsService(string? dataDirectory = null)
    {
        var dir = AppDataPaths.ResolveDataDirectory(dataDirectory);
        _settingsPath = Path.Combine(dir, "settings.json");
        Load();
    }

    public void Load()
    {
        if (!File.Exists(_settingsPath))
        {
            _settings = new AppSettings();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            loaded ??= new AppSettings();
            loaded.Normalize();
            _settings = loaded;
        }
        catch
        {
            // ?????????????????????????????
            _settings = new AppSettings();
        }
    }

    public void Save()
    {
        _settings.Normalize();

        var json = JsonSerializer.Serialize(_settings, JsonOptions);
        var tempPath = _settingsPath + ".tmp";
        File.WriteAllText(tempPath, json);

        if (File.Exists(_settingsPath))
            File.Replace(tempPath, _settingsPath, null);
        else
            File.Move(tempPath, _settingsPath);

        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Update(Action<AppSettings> update)
    {
        update(_settings);
        Save();
    }
}



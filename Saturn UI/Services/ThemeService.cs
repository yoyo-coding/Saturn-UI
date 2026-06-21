using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace SaturnUI.Services;

public static class ThemeDefinitions
{
    public static readonly IReadOnlyDictionary<string, string> DisplayNames = new Dictionary<string, string>
    {
        ["DeepSpace"] = "深空黑",
        ["Daylight"] = "极昼白",
        ["StarryBlue"] = "星夜蓝",
        ["AuroraPurple"] = "极光紫",
        ["GlacierCyan"] = "冰川青",
        ["NebulaPink"] = "星云粉",
        ["MidnightIndigo"] = "午夜靛蓝"
    };

    public static readonly IReadOnlyList<string> ThemeKeys = new List<string>
    {
        "DeepSpace", "Daylight", "StarryBlue", "AuroraPurple",
        "GlacierCyan", "NebulaPink", "MidnightIndigo"
    };
}

public class ThemeService
{
    private readonly SettingsService _settingsService;
    private ResourceDictionary? _themeDictionary;

    public event EventHandler<string>? ThemeChanged;

    public ThemeService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Initialize()
    {
        // Load the Theme.axaml dictionary directly
        try
        {
            _themeDictionary = AvaloniaXamlLoader.Load(
                new Uri("avares://SaturnUI/Themes/Theme.axaml")) as ResourceDictionary;
        }
        catch { /* ignore load errors */ }

        ApplyTheme(_settingsService.Settings.Theme);

        _settingsService.SettingsChanged += (_, _) =>
        {
            ApplyTheme(_settingsService.Settings.Theme);
        };
    }

    public void ApplyTheme(string themeKey)
    {
        if (_themeDictionary == null || App.Current == null)
            return;

        // Find the theme dictionary by key
        if (_themeDictionary.TryGetResource(themeKey, ThemeVariant.Default, out var themeResource)
            && themeResource is ResourceDictionary themeDict)
        {
            // Copy all brushes from the theme dictionary to app resources
            foreach (var key in themeDict.Keys)
            {
                if (themeDict.TryGetResource(key, ThemeVariant.Default, out var value))
                {
                    App.Current.Resources[key] = value;
                }
            }

            // Set the FluentTheme variant for Light/Daylight themes
            var isLight = themeKey == "Daylight";
            App.Current.RequestedThemeVariant = isLight ? ThemeVariant.Light : ThemeVariant.Dark;

            ThemeChanged?.Invoke(this, themeKey);
        }
    }

    public string GetDisplayName(string themeKey)
    {
        return ThemeDefinitions.DisplayNames.TryGetValue(themeKey, out var name)
            ? name
            : themeKey;
    }
}

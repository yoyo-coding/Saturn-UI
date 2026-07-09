using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
    public static readonly IReadOnlySet<string> LightThemeKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        "Daylight"
    };

    public static bool IsLightTheme(string themeKey) => LightThemeKeys.Contains(themeKey);
}

public class ThemeService
{
    private readonly SettingsService _settingsService;
    private ResourceDictionary? _themeDictionary;
    private string? _currentThemeKey;  // 跟踪当前应用的主题,避免重复应用
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

        // 关键优化: 仅在主题真正变化时才重新应用
        // 否则 SettingsService.Save() 触发 SettingsChanged 会导致按钮闪烁
        // (每次 ApplyTheme 都会重新赋值所有 brushes,触发所有 DynamicResource 引用者重新查询)
        if (_currentThemeKey == themeKey)
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
            var isLight = ThemeDefinitions.IsLightTheme(themeKey);
            App.Current.RequestedThemeVariant = isLight ? ThemeVariant.Light : ThemeVariant.Dark;
            ApplyNeutralSurfacePalette(isLight);

            _currentThemeKey = themeKey;  // 记录当前主题
            ThemeChanged?.Invoke(this, themeKey);
        }
    }

    /// <summary>
    /// Keep large surfaces neutral, but tint them with a low-opacity layer of the current theme color.
    /// The visual base remains white/black for readability while saturated themes still keep their identity.
    /// </summary>
    private static void ApplyNeutralSurfacePalette(bool isLight)
    {
        if (App.Current == null)
            return;

        var themeColor = GetThemeColor("M3Primary", isLight ? "#1A56DB" : "#A8C7FA");

        if (isLight)
        {
            SetBrush("M3Background", Blend("#FFFFFF", themeColor, 0.025));
            SetBrush("M3OnBackground", "#1B1B1F");
            SetBrush("M3Surface", Blend("#FFFFFF", themeColor, 0.020));
            SetBrush("M3OnSurface", "#1B1B1F");
            SetBrush("M3SurfaceVariant", Blend("#E6E4EA", themeColor, 0.080));
            SetBrush("M3OnSurfaceVariant", "#5F5E66");
            SetBrush("M3SurfaceContainerLowest", Blend("#FFFFFF", themeColor, 0.015));
            SetBrush("M3SurfaceContainerLow", Blend("#FAFAFB", themeColor, 0.035));
            SetBrush("M3SurfaceContainer", Blend("#F4F4F6", themeColor, 0.055));
            SetBrush("M3SurfaceContainerHigh", Blend("#EEEEF1", themeColor, 0.075));
            SetBrush("M3SurfaceContainerHighest", Blend("#E7E7EB", themeColor, 0.095));
            SetBrush("M3Outline", Blend("#79777F", themeColor, 0.040));
            SetBrush("M3OutlineVariant", Blend("#CAC9CF", themeColor, 0.080));
            SetBrush("M3InverseSurface", "#303034");
            SetBrush("M3InverseOnSurface", "#F3F3F5");
            SetBrush("M3AiBubble", Blend("#F6F6F8", themeColor, 0.060));
            SetBrush("M3OnAiBubble", "#1B1B1F");
            SetBrush("M3CodeBackground", Blend("#F8F8FA", themeColor, 0.035));
            SetBrush("M3CodeBorder", Blend("#D6D6DA", themeColor, 0.070));
            SetBrush("M3InlineCodeBackground", Blend("#EBEBEF", themeColor, 0.070));
            SetBrush("M3HoverOverlay", WithAlpha(themeColor, 0.100));
            SetBrush("M3FocusOverlay", WithAlpha(themeColor, 0.140));
            return;
        }

        SetBrush("M3Background", Blend("#000000", themeColor, 0.045));
        SetBrush("M3OnBackground", "#F2F2F3");
        SetBrush("M3Surface", Blend("#000000", themeColor, 0.040));
        SetBrush("M3OnSurface", "#F2F2F3");
        SetBrush("M3SurfaceVariant", Blend("#424247", themeColor, 0.090));
        SetBrush("M3OnSurfaceVariant", "#C8C7CF");
        SetBrush("M3SurfaceContainerLowest", Blend("#000000", themeColor, 0.020));
        SetBrush("M3SurfaceContainerLow", Blend("#0A0A0B", themeColor, 0.055));
        SetBrush("M3SurfaceContainer", Blend("#121214", themeColor, 0.075));
        SetBrush("M3SurfaceContainerHigh", Blend("#1C1C1F", themeColor, 0.095));
        SetBrush("M3SurfaceContainerHighest", Blend("#26272A", themeColor, 0.115));
        SetBrush("M3Outline", Blend("#909098", themeColor, 0.040));
        SetBrush("M3OutlineVariant", Blend("#3E3F44", themeColor, 0.080));
        SetBrush("M3InverseSurface", "#E5E5E7");
        SetBrush("M3InverseOnSurface", "#303034");
        SetBrush("M3AiBubble", Blend("#18181B", themeColor, 0.085));
        SetBrush("M3OnAiBubble", "#F2F2F3");
        SetBrush("M3CodeBackground", Blend("#08080A", themeColor, 0.045));
        SetBrush("M3CodeBorder", Blend("#38393D", themeColor, 0.070));
        SetBrush("M3InlineCodeBackground", Blend("#25262A", themeColor, 0.090));
        SetBrush("M3HoverOverlay", WithAlpha(themeColor, 0.120));
        SetBrush("M3FocusOverlay", WithAlpha(themeColor, 0.160));
    }

    private static Color GetThemeColor(string key, string fallback)
    {
        if (App.Current?.Resources.TryGetResource(key, ThemeVariant.Default, out var resource) == true
            && resource is SolidColorBrush brush)
        {
            return brush.Color;
        }

        return Color.Parse(fallback);
    }

    private static Color Blend(string baseColor, Color overlayColor, double overlayOpacity)
    {
        return Blend(Color.Parse(baseColor), overlayColor, overlayOpacity);
    }

    private static Color Blend(Color baseColor, Color overlayColor, double overlayOpacity)
    {
        var alpha = Math.Clamp(overlayOpacity, 0, 1);
        return Color.FromRgb(
            BlendChannel(baseColor.R, overlayColor.R, alpha),
            BlendChannel(baseColor.G, overlayColor.G, alpha),
            BlendChannel(baseColor.B, overlayColor.B, alpha));
    }

    private static byte BlendChannel(byte baseChannel, byte overlayChannel, double overlayOpacity)
    {
        return (byte)Math.Round(baseChannel * (1 - overlayOpacity) + overlayChannel * overlayOpacity);
    }

    private static Color WithAlpha(Color color, double opacity)
    {
        return Color.FromArgb((byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255), color.R, color.G, color.B);
    }

    private static void SetBrush(string key, string color)
    {
        SetBrush(key, Color.Parse(color));
    }

    private static void SetBrush(string key, Color color)
    {
        App.Current!.Resources[key] = new SolidColorBrush(color);
    }

    public string GetDisplayName(string themeKey)
    {
        return ThemeDefinitions.DisplayNames.TryGetValue(themeKey, out var name)
            ? name
            : themeKey;
    }
}

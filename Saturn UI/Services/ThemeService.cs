using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace SaturnUI.Services;

public class ThemeService
{
    private readonly SettingsService _settingsService;
    private string? _currentPaletteKey;

    public event EventHandler<bool>? ThemeChanged;

    public ThemeService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Initialize()
    {
        ApplyTheme(_settingsService.Settings.AccentColor, _settingsService.Settings.UseLightTheme);

        _settingsService.SettingsChanged += (_, _) =>
        {
            ApplyTheme(_settingsService.Settings.AccentColor, _settingsService.Settings.UseLightTheme);
        };
    }

    public void ApplyTheme(string accentColor, bool useLightTheme)
    {
        if (App.Current == null)
            return;

        var normalizedAccent = DynamicColorPalette.NormalizeHexColor(accentColor);
        var paletteKey = $"{normalizedAccent}|{useLightTheme}";
        if (_currentPaletteKey == paletteKey)
            return;

        var palette = DynamicColorPalette.Create(normalizedAccent, useLightTheme);
        App.Current.RequestedThemeVariant = useLightTheme ? ThemeVariant.Light : ThemeVariant.Dark;

        SetBrush("M3Primary", palette.Primary);
        SetBrush("M3OnPrimary", palette.OnPrimary);
        SetBrush("M3PrimaryContainer", palette.PrimaryContainer);
        SetBrush("M3OnPrimaryContainer", palette.OnPrimaryContainer);
        SetBrush("M3Secondary", palette.Secondary);
        SetBrush("M3OnSecondary", palette.OnSecondary);
        SetBrush("M3SecondaryContainer", palette.SecondaryContainer);
        SetBrush("M3OnSecondaryContainer", palette.OnSecondaryContainer);
        SetBrush("M3Tertiary", palette.Tertiary);
        SetBrush("M3OnTertiary", palette.OnTertiary);
        SetBrush("M3TertiaryContainer", palette.TertiaryContainer);
        SetBrush("M3OnTertiaryContainer", palette.OnTertiaryContainer);
        SetBrush("M3Error", palette.Error);
        SetBrush("M3OnError", palette.OnError);
        SetBrush("M3ErrorContainer", palette.ErrorContainer);
        SetBrush("M3OnErrorContainer", palette.OnErrorContainer);
        SetBrush("M3Background", palette.Background);
        SetBrush("M3OnBackground", palette.OnBackground);
        SetBrush("M3Surface", palette.Surface);
        SetBrush("M3OnSurface", palette.OnSurface);
        SetBrush("M3SurfaceVariant", palette.SurfaceVariant);
        SetBrush("M3OnSurfaceVariant", palette.OnSurfaceVariant);
        SetBrush("M3SurfaceContainerLowest", palette.SurfaceContainerLowest);
        SetBrush("M3SurfaceContainerLow", palette.SurfaceContainerLow);
        SetBrush("M3SurfaceContainer", palette.SurfaceContainer);
        SetBrush("M3SurfaceContainerHigh", palette.SurfaceContainerHigh);
        SetBrush("M3SurfaceContainerHighest", palette.SurfaceContainerHighest);
        SetBrush("M3Outline", palette.Outline);
        SetBrush("M3OutlineVariant", palette.OutlineVariant);
        SetBrush("M3InverseSurface", palette.InverseSurface);
        SetBrush("M3InverseOnSurface", palette.InverseOnSurface);
        SetBrush("M3InversePrimary", palette.InversePrimary);
        SetBrush("M3UserBubble", palette.UserBubble);
        SetBrush("M3OnUserBubble", palette.OnUserBubble);
        SetBrush("M3AiBubble", palette.AiBubble);
        SetBrush("M3OnAiBubble", palette.OnAiBubble);
        SetBrush("M3CodeBackground", palette.CodeBackground);
        SetBrush("M3CodeBorder", palette.CodeBorder);
        SetBrush("M3InlineCodeBackground", palette.InlineCodeBackground);
        SetBrush("M3HoverOverlay", palette.HoverOverlay);
        SetBrush("M3FocusOverlay", palette.FocusOverlay);
        SetBrush("M3FilledHover", palette.FilledHover);
        SetBrush("M3FilledPressed", palette.FilledPressed);
        SetBrush("M3FilledTonalHover", palette.FilledTonalHover);
        SetBrush("M3FilledTonalPressed", palette.FilledTonalPressed);
        SetBrush("M3Scrim", palette.Scrim);
        SetBrush("M3Shadow", palette.Shadow);

        _currentPaletteKey = paletteKey;
        ThemeChanged?.Invoke(this, useLightTheme);
    }

    private static void SetBrush(string key, Color color)
    {
        App.Current!.Resources[key] = new SolidColorBrush(color);
    }
}

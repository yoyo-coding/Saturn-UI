using System;
using Avalonia.Media;

namespace SaturnUI.Services;

public sealed record DynamicPalette(
    Color Primary,
    Color OnPrimary,
    Color PrimaryContainer,
    Color OnPrimaryContainer,
    Color Secondary,
    Color OnSecondary,
    Color SecondaryContainer,
    Color OnSecondaryContainer,
    Color Tertiary,
    Color OnTertiary,
    Color TertiaryContainer,
    Color OnTertiaryContainer,
    Color Error,
    Color OnError,
    Color ErrorContainer,
    Color OnErrorContainer,
    Color Background,
    Color OnBackground,
    Color Surface,
    Color OnSurface,
    Color SurfaceVariant,
    Color OnSurfaceVariant,
    Color SurfaceContainerLowest,
    Color SurfaceContainerLow,
    Color SurfaceContainer,
    Color SurfaceContainerHigh,
    Color SurfaceContainerHighest,
    Color Outline,
    Color OutlineVariant,
    Color InverseSurface,
    Color InverseOnSurface,
    Color InversePrimary,
    Color UserBubble,
    Color OnUserBubble,
    Color AiBubble,
    Color OnAiBubble,
    Color CodeBackground,
    Color CodeBorder,
    Color InlineCodeBackground,
    Color HoverOverlay,
    Color FocusOverlay,
    Color FilledHover,
    Color FilledPressed,
    Color FilledTonalHover,
    Color FilledTonalPressed,
    Color Scrim,
    Color Shadow);

public static class DynamicColorPalette
{
    public const string FallbackSeedColor = "#6750A4";

    public static string NormalizeHexColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return FallbackSeedColor;

        var hex = value.Trim();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex[2..];
        if (hex.StartsWith('#'))
            hex = hex[1..];

        if (hex.Length == 3)
        {
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        }
        else if (hex.Length == 8)
        {
            hex = hex[2..];
        }

        if (hex.Length != 6)
            return FallbackSeedColor;

        foreach (var ch in hex)
        {
            if (!Uri.IsHexDigit(ch))
                return FallbackSeedColor;
        }

        return $"#{hex.ToUpperInvariant()}";
    }

    public static Color ParseSeed(string? value)
    {
        return Color.Parse(NormalizeHexColor(value));
    }

    public static DynamicPalette Create(string? seedHex, bool useLightTheme)
    {
        var seed = ParseSeed(seedHex);
        var (h, s, l) = ToHsl(seed);
        s = Math.Clamp(Math.Max(s, 0.38), 0, 0.86);

        var secondaryHue = ShiftHue(h, 24);
        var tertiaryHue = ShiftHue(h, -52);
        var neutralHue = h;
        var secondarySaturation = Math.Clamp(s * 0.42, 0.18, 0.44);
        var tertiarySaturation = Math.Clamp(s * 0.55, 0.24, 0.58);
        var neutralSaturation = Math.Clamp(s * 0.10, 0.025, 0.090);
        var neutralVariantSaturation = Math.Clamp(s * 0.14, 0.040, 0.130);

        if (useLightTheme)
        {
            var primary = FromHsl(h, s, 0.40);
            var primaryContainer = FromHsl(h, Math.Clamp(s * 0.72, 0.22, 0.64), 0.90);
            var secondary = FromHsl(secondaryHue, secondarySaturation, 0.42);
            var secondaryContainer = FromHsl(secondaryHue, secondarySaturation, 0.88);
            var tertiary = FromHsl(tertiaryHue, tertiarySaturation, 0.42);
            var tertiaryContainer = FromHsl(tertiaryHue, tertiarySaturation, 0.88);
            var surface = FromHsl(neutralHue, neutralSaturation, 0.985);
            var surfaceContainer = FromHsl(neutralHue, neutralSaturation, 0.935);
            var surfaceContainerHigh = FromHsl(neutralHue, neutralSaturation, 0.905);
            var surfaceContainerHighest = FromHsl(neutralHue, neutralSaturation, 0.875);
            var surfaceVariant = FromHsl(neutralHue, neutralVariantSaturation, 0.900);
            var outlineVariant = FromHsl(neutralHue, neutralVariantSaturation, 0.765);

            return new DynamicPalette(
                primary, BestTextOn(primary),
                primaryContainer, BestTextOn(primaryContainer),
                secondary, BestTextOn(secondary),
                secondaryContainer, BestTextOn(secondaryContainer),
                tertiary, BestTextOn(tertiary),
                tertiaryContainer, BestTextOn(tertiaryContainer),
                Color.Parse("#BA1A1A"), Colors.White,
                Color.Parse("#FFDAD6"), Color.Parse("#410002"),
                surface, Color.Parse("#1B1B1F"),
                surface, Color.Parse("#1B1B1F"),
                surfaceVariant, Color.Parse("#46464F"),
                FromHsl(neutralHue, neutralSaturation, 1.000),
                FromHsl(neutralHue, neutralSaturation, 0.965),
                surfaceContainer,
                surfaceContainerHigh,
                surfaceContainerHighest,
                FromHsl(neutralHue, neutralVariantSaturation, 0.485),
                outlineVariant,
                Color.Parse("#303034"), Color.Parse("#F3F3F5"),
                FromHsl(h, s, 0.78),
                primaryContainer, BestTextOn(primaryContainer),
                Blend(surfaceContainer, primary, 0.035), Color.Parse("#1B1B1F"),
                FromHsl(neutralHue, neutralSaturation, 0.965), outlineVariant,
                FromHsl(neutralHue, neutralVariantSaturation, 0.925),
                WithAlpha(primary, 0.10), WithAlpha(primary, 0.14),
                Blend(primary, Colors.White, 0.10), Blend(primary, Colors.Black, 0.10),
                Blend(secondaryContainer, Colors.White, 0.16), Blend(secondaryContainer, Colors.Black, 0.08),
                Colors.Black, Colors.Black);
        }

        var darkPrimary = FromHsl(h, s, 0.76);
        var darkPrimaryContainer = FromHsl(h, Math.Clamp(s * 0.72, 0.24, 0.66), 0.25);
        var darkSecondary = FromHsl(secondaryHue, secondarySaturation, 0.76);
        var darkSecondaryContainer = FromHsl(secondaryHue, secondarySaturation, 0.28);
        var darkTertiary = FromHsl(tertiaryHue, tertiarySaturation, 0.76);
        var darkTertiaryContainer = FromHsl(tertiaryHue, tertiarySaturation, 0.28);
        var darkSurface = FromHsl(neutralHue, neutralSaturation, 0.055);
        var darkSurfaceContainer = FromHsl(neutralHue, neutralSaturation, 0.115);
        var darkSurfaceContainerHigh = FromHsl(neutralHue, neutralSaturation, 0.155);
        var darkSurfaceContainerHighest = FromHsl(neutralHue, neutralSaturation, 0.195);
        var darkSurfaceVariant = FromHsl(neutralHue, neutralVariantSaturation, 0.285);
        var darkOutlineVariant = FromHsl(neutralHue, neutralVariantSaturation, 0.300);

        return new DynamicPalette(
            darkPrimary, BestTextOn(darkPrimary),
            darkPrimaryContainer, BestTextOn(darkPrimaryContainer),
            darkSecondary, BestTextOn(darkSecondary),
            darkSecondaryContainer, BestTextOn(darkSecondaryContainer),
            darkTertiary, BestTextOn(darkTertiary),
            darkTertiaryContainer, BestTextOn(darkTertiaryContainer),
            Color.Parse("#FFB4AB"), Color.Parse("#690005"),
            Color.Parse("#93000A"), Color.Parse("#FFDAD6"),
            darkSurface, Color.Parse("#F2F2F3"),
            darkSurface, Color.Parse("#F2F2F3"),
            darkSurfaceVariant, Color.Parse("#CAC9D1"),
            FromHsl(neutralHue, neutralSaturation, 0.025),
            FromHsl(neutralHue, neutralSaturation, 0.085),
            darkSurfaceContainer,
            darkSurfaceContainerHigh,
            darkSurfaceContainerHighest,
            FromHsl(neutralHue, neutralVariantSaturation, 0.595),
            darkOutlineVariant,
            Color.Parse("#E5E5E7"), Color.Parse("#303034"),
            FromHsl(h, s, 0.40),
            darkPrimaryContainer, BestTextOn(darkPrimaryContainer),
            Blend(darkSurfaceContainer, darkPrimary, 0.055), Color.Parse("#F2F2F3"),
            FromHsl(neutralHue, neutralSaturation, 0.040), darkOutlineVariant,
            FromHsl(neutralHue, neutralVariantSaturation, 0.165),
            WithAlpha(darkPrimary, 0.12), WithAlpha(darkPrimary, 0.16),
            Blend(darkPrimary, Colors.White, 0.10), Blend(darkPrimary, Colors.Black, 0.12),
            Blend(darkSecondaryContainer, Colors.White, 0.10), Blend(darkSecondaryContainer, Colors.Black, 0.10),
            Colors.Black, Colors.Black);
    }

    private static Color BestTextOn(Color background)
    {
        return RelativeLuminance(background) > 0.48 ? Color.Parse("#111114") : Colors.White;
    }

    private static Color WithAlpha(Color color, double opacity)
    {
        return Color.FromArgb((byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255), color.R, color.G, color.B);
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

    private static double ShiftHue(double hue, double degrees)
    {
        var shifted = hue + degrees / 360d;
        shifted %= 1;
        return shifted < 0 ? shifted + 1 : shifted;
    }

    private static (double H, double S, double L) ToHsl(Color color)
    {
        var r = color.R / 255d;
        var g = color.G / 255d;
        var b = color.B / 255d;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2d;

        if (Math.Abs(max - min) < 0.0001)
            return (0, 0, l);

        var delta = max - min;
        var s = l > 0.5 ? delta / (2d - max - min) : delta / (max + min);
        double h;
        if (Math.Abs(max - r) < 0.0001)
            h = (g - b) / delta + (g < b ? 6 : 0);
        else if (Math.Abs(max - g) < 0.0001)
            h = (b - r) / delta + 2;
        else
            h = (r - g) / delta + 4;

        return (h / 6d, s, l);
    }

    private static Color FromHsl(double h, double s, double l)
    {
        h = ((h % 1) + 1) % 1;
        s = Math.Clamp(s, 0, 1);
        l = Math.Clamp(l, 0, 1);

        if (s <= 0.0001)
        {
            var gray = ToByte(l);
            return Color.FromRgb(gray, gray, gray);
        }

        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        var r = HueToRgb(p, q, h + 1d / 3d);
        var g = HueToRgb(p, q, h);
        var b = HueToRgb(p, q, h - 1d / 3d);
        return Color.FromRgb(ToByte(r), ToByte(g), ToByte(b));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1d / 6d) return p + (q - p) * 6 * t;
        if (t < 1d / 2d) return q;
        if (t < 2d / 3d) return p + (q - p) * (2d / 3d - t) * 6;
        return p;
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Round(Math.Clamp(value, 0, 1) * 255);
    }

    private static double RelativeLuminance(Color color)
    {
        static double Convert(byte channel)
        {
            var value = channel / 255d;
            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Convert(color.R) + 0.7152 * Convert(color.G) + 0.0722 * Convert(color.B);
    }
}

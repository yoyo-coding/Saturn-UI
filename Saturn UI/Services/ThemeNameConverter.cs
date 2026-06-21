using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SaturnUI.Services;

public class ThemeNameConverter : IValueConverter
{
    public static readonly ThemeNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string themeKey)
        {
            return ThemeDefinitions.DisplayNames.TryGetValue(themeKey, out var name)
                ? name
                : themeKey;
        }
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

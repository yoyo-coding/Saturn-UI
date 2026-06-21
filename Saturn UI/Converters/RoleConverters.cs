using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using SaturnUI.Models;

namespace SaturnUI.Converters;

public class RoleAlignmentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MessageRole role)
        {
            return role == MessageRole.User ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }
        return HorizontalAlignment.Left;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RoleBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MessageRole role)
        {
            var brush = role == MessageRole.User
                ? App.Current?.Resources["LynUserMessageBrush"]
                : App.Current?.Resources["LynAiMessageBrush"];
            return brush;
        }
        return App.Current?.Resources["LynAiMessageBrush"];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

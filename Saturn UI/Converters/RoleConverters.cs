using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using SaturnUI.Models;

namespace SaturnUI.Converters;

/// <summary>
/// 将 MessageRole 映射为 HorizontalAlignment
/// </summary>
public sealed class RoleAlignmentConverter : IValueConverter
{
    public static readonly RoleAlignmentConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is MessageRole role && role == MessageRole.User
            ? HorizontalAlignment.Right
            : HorizontalAlignment.Left;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// 将 MessageRole 映射为聊天气泡背景色
/// 优化: 资源键缓存 + 避免反射
/// </summary>
public sealed class RoleBackgroundConverter : IValueConverter
{
    public static readonly RoleBackgroundConverter Instance = new();

    private const string UserBrushKey = "M3UserBubble";
    private const string AiBrushKey = "M3AiBubble";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value is MessageRole.User ? UserBrushKey : AiBrushKey;
        return App.Current?.Resources.TryGetResource(key, Avalonia.Styling.ThemeVariant.Default, out var resource) == true
            ? resource
            : App.Current?.Resources.TryGetResource(AiBrushKey, Avalonia.Styling.ThemeVariant.Default, out var fallback) == true
                ? fallback
                : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

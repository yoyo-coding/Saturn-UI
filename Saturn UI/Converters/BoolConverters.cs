using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace SaturnUI.Converters;

/// <summary>
/// 通用布尔值转换器集合
/// </summary>
public static class BoolConverters
{
    /// <summary>
    /// bool → GridLength,param 为像素宽度
    /// 用法: ConverterParameter="260"
    /// </summary>
    public static readonly IValueConverter StarGridLength =
        new FuncValueConverter<bool, string?, GridLength>((value, param) =>
        {
            if (value && param != null && double.TryParse(param, NumberStyles.Any, CultureInfo.InvariantCulture, out var width))
                return new GridLength(width, GridUnitType.Pixel);
            return new GridLength(0, GridUnitType.Pixel);
        });

    /// <summary>
    /// bool → 可见性
    /// </summary>
    public static readonly IValueConverter ToVisibility =
        new FuncValueConverter<bool, bool>(b => b);

    /// <summary>
    /// bool → 0/1 透明度
    /// </summary>
    public static readonly IValueConverter ToOpacity =
        new FuncValueConverter<bool, double>(b => b ? 1.0 : 0.0);
}

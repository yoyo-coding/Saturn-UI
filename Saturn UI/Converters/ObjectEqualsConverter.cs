using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SaturnUI.Converters;

/// <summary>
/// 比较两个对象是否相等 - 用于会话项选中状态判断
///
/// 用法:
///   IsSelected="{Binding SelectedItem, Converter={x:Static converters:ObjectEqualsConverter.Instance}, ConverterParameter={Binding}}"
///
/// value: 引用源(例如 SelectedSession)
/// parameter: 当前项(例如当前 DataTemplate 绑定的 Session)
/// </summary>
public sealed class ObjectEqualsConverter : IValueConverter
{
    public static readonly ObjectEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null && parameter is null) return true;
        if (value is null || parameter is null) return false;

        // 优先用引用比较(对象身份),性能最优
        if (ReferenceEquals(value, parameter)) return true;

        // 退化为值比较
        return value.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

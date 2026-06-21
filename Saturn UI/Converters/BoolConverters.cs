using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia;
using Avalonia.Controls;

namespace SaturnUI.Converters;

public static class BoolConverters
{
    public static readonly IValueConverter StarGridLength = new FuncValueConverter<bool, string, GridLength>((value, param) =>
    {
        if (value && param != null && double.TryParse(param, out var width))
            return new GridLength(width, GridUnitType.Pixel);
        return new GridLength(0, GridUnitType.Pixel);
    });
}

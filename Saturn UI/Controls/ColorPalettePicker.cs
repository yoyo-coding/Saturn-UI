using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SaturnUI.Services;

namespace SaturnUI.Controls;

public class ColorPalettePicker : Control
{
    public static readonly StyledProperty<string> SelectedColorHexProperty =
        AvaloniaProperty.Register<ColorPalettePicker, string>(
            nameof(SelectedColorHex),
            DynamicColorPalette.FallbackSeedColor,
            defaultBindingMode: BindingMode.TwoWay);

    private const int CacheWidth = 360;
    private const int CacheHeight = 180;
    private WriteableBitmap? _paletteBitmap;
    private Point _selectorPosition = new(CacheWidth * 0.75, CacheHeight * 0.32);
    private bool _updatingFromPointer;

    public string SelectedColorHex
    {
        get => GetValue(SelectedColorHexProperty);
        set => SetValue(SelectedColorHexProperty, value);
    }

    static ColorPalettePicker()
    {
        AffectsRender<ColorPalettePicker>(SelectedColorHexProperty);
    }

    public ColorPalettePicker()
    {
        ClipToBounds = true;
        Cursor = new Cursor(StandardCursorType.Cross);
        MinHeight = 144;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedColorHexProperty && !_updatingFromPointer)
        {
            _selectorPosition = PositionFromColor(DynamicColorPalette.ParseSeed(SelectedColorHex));
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        EnsurePaletteBitmap();

        var rect = new Rect(Bounds.Size);
        if (_paletteBitmap is not null)
            context.DrawImage(_paletteBitmap, rect);

        var cornerRadius = MaterialShapeTokens.ExtraLarge;
        context.DrawRectangle(
            brush: null,
            pen: new Pen(Application.Current?.TryFindResource("M3OutlineVariant", out var outline) == true && outline is IBrush outlineBrush
                ? outlineBrush
                : Brushes.Gray, 1),
            rect: rect.Deflate(0.5),
            radiusX: cornerRadius.TopLeft,
            radiusY: cornerRadius.TopLeft);

        var selector = new Point(
            Math.Clamp(_selectorPosition.X / CacheWidth * Math.Max(Bounds.Width, 1), 0, Bounds.Width),
            Math.Clamp(_selectorPosition.Y / CacheHeight * Math.Max(Bounds.Height, 1), 0, Bounds.Height));

        context.DrawEllipse(null, new Pen(Brushes.Black, 4), selector, 8, 8);
        context.DrawEllipse(null, new Pen(Brushes.White, 2), selector, 7, 7);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        e.Pointer.Capture(this);
        UpdateColorFromPointer(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            UpdateColorFromPointer(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        e.Pointer.Capture(null);
        UpdateColorFromPointer(e.GetPosition(this));
        e.Handled = true;
    }

    private void UpdateColorFromPointer(Point point)
    {
        var width = Math.Max(Bounds.Width, 1);
        var height = Math.Max(Bounds.Height, 1);
        var x = Math.Clamp(point.X / width, 0, 1);
        var y = Math.Clamp(point.Y / height, 0, 1);

        _selectorPosition = new Point(x * CacheWidth, y * CacheHeight);
        var color = ColorAt(x, y);
        var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        _updatingFromPointer = true;
        try
        {
            SelectedColorHex = hex;
        }
        finally
        {
            _updatingFromPointer = false;
        }

        InvalidateVisual();
    }

    private static Point PositionFromColor(Color color)
    {
        var (h, s, v) = ToHsv(color);
        var y = v >= 0.98
            ? s * 0.5
            : 0.5 + (1 - v) * 0.5;

        return new Point(h * CacheWidth, Math.Clamp(y, 0, 1) * CacheHeight);
    }

    private static void EnsurePremultiplied(ref byte r, ref byte g, ref byte b, byte a)
    {
        if (a == 255)
            return;

        r = (byte)(r * a / 255);
        g = (byte)(g * a / 255);
        b = (byte)(b * a / 255);
    }

    private void EnsurePaletteBitmap()
    {
        if (_paletteBitmap is not null)
            return;

        _paletteBitmap = new WriteableBitmap(
            new PixelSize(CacheWidth, CacheHeight),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using var frameBuffer = _paletteBitmap.Lock();
        var pixels = new int[CacheWidth];
        var radius = MaterialShapeTokens.ExtraLarge.TopLeft;

        for (var y = 0; y < CacheHeight; y++)
        {
            var normalizedY = CacheHeight <= 1 ? 0 : y / (double)(CacheHeight - 1);
            for (var x = 0; x < CacheWidth; x++)
            {
                var normalizedX = CacheWidth <= 1 ? 0 : x / (double)(CacheWidth - 1);
                var color = ColorAt(normalizedX, normalizedY);
                var r = color.R;
                var g = color.G;
                var b = color.B;
                var a = IsInsideRoundedRect(x + 0.5, y + 0.5, CacheWidth, CacheHeight, radius)
                    ? color.A
                    : (byte)0;
                EnsurePremultiplied(ref r, ref g, ref b, a);
                pixels[x] = (a << 24) | (r << 16) | (g << 8) | b;
            }

            Marshal.Copy(pixels, 0, frameBuffer.Address + y * frameBuffer.RowBytes, CacheWidth);
        }
    }

    private static bool IsInsideRoundedRect(double x, double y, double width, double height, double radius)
    {
        radius = Math.Min(Math.Clamp(radius, 0, Math.Min(width, height) / 2), Math.Min(width, height) / 2);
        if (x >= radius && x <= width - radius)
            return true;
        if (y >= radius && y <= height - radius)
            return true;

        var cx = x < radius ? radius : width - radius;
        var cy = y < radius ? radius : height - radius;
        var dx = x - cx;
        var dy = y - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static Color ColorAt(double x, double y)
    {
        var hue = Math.Clamp(x, 0, 1);
        var position = Math.Clamp(y, 0, 1);
        var pure = FromHsv(hue, 1, 1);

        return position <= 0.5
            ? Blend(Colors.White, pure, position * 2)
            : Blend(pure, Colors.Black, (position - 0.5) * 2);
    }

    private static Color Blend(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            (byte)Math.Round(from.R + (to.R - from.R) * amount),
            (byte)Math.Round(from.G + (to.G - from.G) * amount),
            (byte)Math.Round(from.B + (to.B - from.B) * amount));
    }

    private static Color FromHsv(double h, double s, double v)
    {
        h = ((h % 1) + 1) % 1;
        s = Math.Clamp(s, 0, 1);
        v = Math.Clamp(v, 0, 1);

        var c = v * s;
        var hp = h * 6;
        var x = c * (1 - Math.Abs(hp % 2 - 1));
        var m = v - c;

        var (r, g, b) = hp switch
        {
            < 1 => (c, x, 0d),
            < 2 => (x, c, 0d),
            < 3 => (0d, c, x),
            < 4 => (0d, x, c),
            < 5 => (x, 0d, c),
            _ => (c, 0d, x)
        };

        return Color.FromRgb(ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    private static (double H, double S, double V) ToHsv(Color color)
    {
        var r = color.R / 255d;
        var g = color.G / 255d;
        var b = color.B / 255d;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        double h;
        if (delta <= 0.0001)
            h = 0;
        else if (Math.Abs(max - r) < 0.0001)
            h = ((g - b) / delta + (g < b ? 6 : 0)) / 6;
        else if (Math.Abs(max - g) < 0.0001)
            h = ((b - r) / delta + 2) / 6;
        else
            h = ((r - g) / delta + 4) / 6;

        var s = max <= 0.0001 ? 0 : delta / max;
        return (h, s, max);
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Round(Math.Clamp(value, 0, 1) * 255);
    }
}

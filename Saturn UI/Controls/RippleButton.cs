using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace SaturnUI.Controls;

/// <summary>
/// M3 涟漪按钮 - 点击时从按压点向四周扩散的圆形涟漪动效
///
/// 实现:
///   1. 涟漪动效逻辑委托给 <see cref="RippleAnimator"/>,支持复用
///   2. 容器继承按钮 CornerRadius,矩形遮罩与按钮形状一致
///   3. ScaleTransform 缩放 + Opacity 衰减,GPU 加速
/// </summary>
public class RippleButton : Button
{
    public static readonly StyledProperty<IBrush?> RippleBrushProperty =
        AvaloniaProperty.Register<RippleButton, IBrush?>(nameof(RippleBrush));

    public static readonly StyledProperty<double> RippleOpacityProperty =
        AvaloniaProperty.Register<RippleButton, double>(nameof(RippleOpacity), 0.32);

    public static readonly StyledProperty<TimeSpan> RippleDurationProperty =
        AvaloniaProperty.Register<RippleButton, TimeSpan>(
            nameof(RippleDuration), TimeSpan.FromMilliseconds(550));

    public IBrush? RippleBrush
    {
        get => GetValue(RippleBrushProperty);
        set => SetValue(RippleBrushProperty, value);
    }

    public double RippleOpacity
    {
        get => GetValue(RippleOpacityProperty);
        set => SetValue(RippleOpacityProperty, value);
    }

    public TimeSpan RippleDuration
    {
        get => GetValue(RippleDurationProperty);
        set => SetValue(RippleDurationProperty, value);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var position = e.GetCurrentPoint(this).Position;
        RippleAnimator.Play(this, position, RippleBrush, RippleOpacity, RippleDuration);
    }
}

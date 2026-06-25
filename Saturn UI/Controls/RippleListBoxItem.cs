using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace SaturnUI.Controls;

/// <summary>
/// 带涟漪动效的 ListBoxItem
///
/// 为什么需要这个类:
///   - 附加属性 RippleEffect.IsEnabled 的事件订阅会被 ListBox 内部的 Handled 标记拦截
///   - 重写 OnPointerPressed 是最可靠的方式,ListBoxItem 自身的 OnPointerPressed 不会标记 Handled
///
/// 用法 (XAML):
///   &lt;ListBox.ItemContainerTheme&gt;
///     &lt;ControlTheme TargetType="controls:RippleListBoxItem" ...&gt;
///   &lt;/ListBox.ItemContainerTheme&gt;
///
/// 可配置:
///   - RippleOpacity (默认 0.24)
///   - RippleDuration (默认 550ms)
///   - RippleBrush (默认 null,即使用 Foreground)
/// </summary>
public class RippleListBoxItem : ListBoxItem
{
    public static readonly StyledProperty<double> RippleOpacityProperty =
        AvaloniaProperty.Register<RippleListBoxItem, double>(nameof(RippleOpacity), 0.24);

    public static readonly StyledProperty<TimeSpan> RippleDurationProperty =
        AvaloniaProperty.Register<RippleListBoxItem, TimeSpan>(
            nameof(RippleDuration), TimeSpan.FromMilliseconds(550));

    public static readonly StyledProperty<IBrush?> RippleBrushProperty =
        AvaloniaProperty.Register<RippleListBoxItem, IBrush?>(nameof(RippleBrush));

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

    public IBrush? RippleBrush
    {
        get => GetValue(RippleBrushProperty);
        set => SetValue(RippleBrushProperty, value);
    }

    /// <summary>
    /// 拦截 PointerPressed - 触发涟漪
    /// </summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var position = e.GetCurrentPoint(this).Position;
            RippleAnimator.Play(this, position, RippleBrush, RippleOpacity, RippleDuration);
        }

        // 调用基类保留 ListBox 选择行为
        base.OnPointerPressed(e);
    }
}

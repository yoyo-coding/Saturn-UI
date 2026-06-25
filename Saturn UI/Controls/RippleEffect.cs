using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace SaturnUI.Controls;

/// <summary>
/// 涟漪效果附加属性 - 让任何控件可触发涟漪动效
///
/// 用法 (XAML):
///   &lt;ListBoxItem controls:RippleEffect.IsEnabled="True" /&gt;
///   &lt;Border controls:RippleEffect.IsEnabled="True" /&gt;
///
/// 默认行为:
///   - 颜色: target.Foreground
///   - 透明度: 0.32
///   - 时长: 550ms
///   - 容器圆角: 继承 target.CornerRadius (TemplatedControl/Border)
///
/// 可自定义 (XAML):
///   controls:RippleEffect.Opacity="0.5"
///   controls:RippleEffect.Duration="0:0:0.3"
///   controls:RippleEffect.Brush="{DynamicResource M3Primary}"
/// </summary>
public static class RippleEffect
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(RippleEffect), false);

    public static readonly AttachedProperty<IBrush?> BrushProperty =
        AvaloniaProperty.RegisterAttached<Control, IBrush?>("Brush", typeof(RippleEffect), null);

    public static readonly AttachedProperty<double> OpacityProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("Opacity", typeof(RippleEffect), 0.32);

    public static readonly AttachedProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.RegisterAttached<Control, TimeSpan>("Duration", typeof(RippleEffect), TimeSpan.FromMilliseconds(550));

    static RippleEffect()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>((control, e) =>
        {
            if (e.NewValue is true)
                control.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, handledEventsToo: false);
            else
                control.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        });
    }

    public static void SetIsEnabled(Control obj, bool value) => obj.SetValue(IsEnabledProperty, value);
    public static bool GetIsEnabled(Control obj) => obj.GetValue(IsEnabledProperty);

    public static void SetBrush(Control obj, IBrush? value) => obj.SetValue(BrushProperty, value);
    public static IBrush? GetBrush(Control obj) => obj.GetValue(BrushProperty);

    public static void SetOpacity(Control obj, double value) => obj.SetValue(OpacityProperty, value);
    public static double GetOpacity(Control obj) => obj.GetValue(OpacityProperty);

    public static void SetDuration(Control obj, TimeSpan value) => obj.SetValue(DurationProperty, value);
    public static TimeSpan GetDuration(Control obj) => obj.GetValue(DurationProperty);

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed) return;

        var position = e.GetCurrentPoint(control).Position;
        var brush = GetBrush(control);
        var opacity = GetOpacity(control);
        var duration = GetDuration(control);

        RippleAnimator.Play(control, position, brush, opacity, duration);
    }
}

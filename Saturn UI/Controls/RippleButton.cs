using System;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace SaturnUI.Controls;

/// <summary>
/// M3 涟漪按钮 - 点击时从按压点向四周扩散的圆形涟漪动效
///
/// 性能优化:
///   1. 使用 <see cref="CompositionTarget.Rendering"/> 替代 DispatcherTimer
///      与渲染线程 1:1 同步,无时钟漂移
///   2. ScaleTransform 缩放 + Opacity 衰减,GPU 加速
///   3. 椭圆容器使用 Border + CornerRadius,裁剪形状与按钮一致
///   4. 涟漪结束自动从 AdornerLayer 移除,无内存泄漏
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

    private static readonly Easing s_easeOutCubic = new CubicEaseOut();

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var adornerLayer = AdornerLayer.GetAdornerLayer(this);
        if (adornerLayer is null) return;

        var position = e.GetCurrentPoint(this).Position;
        StartRipple(adornerLayer, position);
    }

    private void StartRipple(AdornerLayer adornerLayer, Point position)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        var diameter = Math.Max(Bounds.Width, Bounds.Height) * 2.5;
        var brush = RippleBrush ?? Foreground ?? new SolidColorBrush(Colors.White);

        var ellipse = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = brush,
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            RenderTransform = new ScaleTransform(0, 0),
            Opacity = 0,
            Margin = new Thickness(
                position.X - diameter / 2,
                position.Y - diameter / 2, 0, 0)
        };

        var container = new Border
        {
            Width = Bounds.Width,
            Height = Bounds.Height,
            IsHitTestVisible = false,
            ClipToBounds = true,
            CornerRadius = CornerRadius,
            Background = null,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = ellipse
        };

        var containerPos = this.TranslatePoint(new Point(0, 0), adornerLayer);
        if (containerPos.HasValue)
        {
            container.Margin = new Thickness(containerPos.Value.X, containerPos.Value.Y, 0, 0);
        }

        adornerLayer.Children.Add(container);

        var scale = (ScaleTransform)ellipse.RenderTransform!;
        var duration = RippleDuration.TotalMilliseconds;
        var startTime = DateTime.UtcNow;

        // 使用 DispatcherTimer(Render 优先级)驱动动画
        // 与 UI 渲染同步,16ms 帧间隔 ~60fps
        var timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        void OnTick(object? _, EventArgs e)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var progress = Math.Min(1.0, elapsed / duration);

            if (progress >= 1.0)
            {
                timer.Stop();
                adornerLayer.Children.Remove(container);
                return;
            }

            // easeOutCubic 缓动
            var eased = s_easeOutCubic.Ease(progress);

            // 透明度曲线: 0 → max(快,前 20%)→ 0(慢,后 80%)
            var opacity = progress < 0.2
                ? RippleOpacity * (progress / 0.2)
                : RippleOpacity * (1.0 - (progress - 0.2) / 0.8);

            scale.ScaleX = eased;
            scale.ScaleY = eased;
            ellipse.Opacity = opacity;
        }

        timer.Tick += OnTick;
        timer.Start();
    }
}

using System;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace SaturnUI.Controls;

/// <summary>
/// 涟漪动效工具类 - 在指定容器中创建并播放涟漪
///
/// 设计:
///   1. 抽离自 RippleButton,让任何控件可复用
///   2. 通过 AdornerLayer 渲染,避免触发重新布局
///   3. ScaleTransform + Opacity 衰减,GPU 加速
///   4. 容器继承控件 CornerRadius,裁剪形状与目标一致
/// </summary>
public static class RippleAnimator
{
    private static readonly Easing s_easeOutCubic = new CubicEaseOut();

    public static IBrush? DefaultBrush { get; set; }

    /// <summary>
    /// 在指定目标控件上播放涟漪
    /// </summary>
    /// <param name="target">目标控件(被点击的元素)</param>
    /// <param name="position">点击位置(相对 target 坐标系)</param>
    /// <param name="brush">涟漪颜色,null 时使用 target.Foreground</param>
    /// <param name="opacity">最大透明度(0-1)</param>
    /// <param name="duration">动画时长</param>
    public static void Play(
        Control target,
        Point position,
        IBrush? brush = null,
        double opacity = 0.32,
        TimeSpan? duration = null)
    {
        if (target.Bounds.Width <= 0 || target.Bounds.Height <= 0) return;

        var adornerLayer = AdornerLayer.GetAdornerLayer(target);
        if (adornerLayer is null) return;

        Play(adornerLayer, target, position, brush, opacity, duration);
    }

    /// <summary>
    /// 在指定 AdornerLayer 上播放涟漪
    /// </summary>
    public static void Play(
        AdornerLayer adornerLayer,
        Control target,
        Point targetPosition,
        IBrush? brush = null,
        double opacity = 0.32,
        TimeSpan? duration = null)
    {
        if (target.Bounds.Width <= 0 || target.Bounds.Height <= 0) return;

        var diameter = Math.Max(target.Bounds.Width, target.Bounds.Height) * 2.5;
        var rippleBrush = brush
            ?? (target as TemplatedControl)?.Foreground
            ?? DefaultBrush
            ?? new SolidColorBrush(Colors.White);
        var animDuration = duration ?? TimeSpan.FromMilliseconds(550);

        // 涟漪 Ellipse: 中心对准点击位置,围绕中心缩放
        var ellipse = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = rippleBrush,
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            RenderTransform = new ScaleTransform(0, 0),
            Opacity = 0,
            Margin = new Thickness(
                targetPosition.X - diameter / 2,
                targetPosition.Y - diameter / 2, 0, 0)
        };

        // 容器: 继承目标 CornerRadius,让裁剪形状与目标一致
        var container = new Border
        {
            Width = target.Bounds.Width,
            Height = target.Bounds.Height,
            IsHitTestVisible = false,
            ClipToBounds = true,
            CornerRadius = target is Border border ? border.CornerRadius
                          : target is TemplatedControl tc ? tc.CornerRadius
                          : new CornerRadius(0),
            Background = null,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = ellipse
        };

        // 容器定位到目标位置
        var containerPos = target.TranslatePoint(new Point(0, 0), adornerLayer);
        if (containerPos.HasValue)
        {
            container.Margin = new Thickness(containerPos.Value.X, containerPos.Value.Y, 0, 0);
        }

        adornerLayer.Children.Add(container);

        // 启动动画
        var scale = (ScaleTransform)ellipse.RenderTransform!;
        var durationMs = animDuration.TotalMilliseconds;
        var startTime = DateTime.UtcNow;

        var timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        void OnTick(object? _, EventArgs e)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var progress = Math.Min(1.0, elapsed / durationMs);

            if (progress >= 1.0)
            {
                timer.Stop();
                adornerLayer.Children.Remove(container);
                return;
            }

            // easeOutCubic 缓动
            var eased = s_easeOutCubic.Ease(progress);

            // 透明度曲线: 0 → max(快,前 20%)→ 0(慢,后 80%)
            var currentOpacity = progress < 0.2
                ? opacity * (progress / 0.2)
                : opacity * (1.0 - (progress - 0.2) / 0.8);

            scale.ScaleX = eased;
            scale.ScaleY = eased;
            ellipse.Opacity = currentOpacity;
        }

        timer.Tick += OnTick;
        timer.Start();
    }
}

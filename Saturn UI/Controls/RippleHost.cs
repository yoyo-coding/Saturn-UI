using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace SaturnUI.Controls;

/// <summary>
/// 继承 Button 的 M3 涟漪按钮,点击时从按压点向四周扩散的圆形涟漪动效
/// 关键修复:
/// - 直接在 AdornerLayer 上渲染 Ellipse,不再包裹 Border 容器
/// - 避免 AdornerLayer 中额外容器与按钮自身可视树的渲染不同步(悬停闪烁)
/// - 涟漪是圆形,自然扩散到按钮外(M3 标准行为),不需要矩形遮罩
/// - ScaleTransform 缩放 + Opacity 衰减,GPU 加速,60fps
/// </summary>
public class RippleButton : Button
{
    public static readonly StyledProperty<IBrush?> RippleBrushProperty =
        AvaloniaProperty.Register<RippleButton, IBrush?>(nameof(RippleBrush));

    public static readonly StyledProperty<double> RippleOpacityProperty =
        AvaloniaProperty.Register<RippleButton, double>(nameof(RippleOpacity), 0.32);

    public static readonly StyledProperty<TimeSpan> RippleDurationProperty =
        AvaloniaProperty.Register<RippleButton, TimeSpan>(nameof(RippleDuration),
            TimeSpan.FromMilliseconds(550));

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

        // 点击位置(相对按钮坐标系)
        var position = e.GetCurrentPoint(this).Position;

        var adornerLayer = AdornerLayer.GetAdornerLayer(this);
        if (adornerLayer is null) return;

        CreateRipple(adornerLayer, position);
    }

    private void CreateRipple(AdornerLayer adornerLayer, Point buttonPosition)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        // 涟漪直径 = 按钮最大边长 * 2.5(确保完全覆盖)
        var diameter = Math.Max(Bounds.Width, Bounds.Height) * 2.5;

        // 涟漪颜色
        var brush = RippleBrush ?? Foreground ?? new SolidColorBrush(Colors.White);

        // 涟漪 Ellipse: 直接渲染,不包裹 Border 容器
        // 圆形天然扩散,无需矩形遮罩
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
        };

        // 直接转换为 AdornerLayer 坐标系,定位 ellipse 中心 = 点击位置
        var clickPos = this.TranslatePoint(buttonPosition, adornerLayer);
        if (!clickPos.HasValue) return;

        ellipse.Margin = new Thickness(
            clickPos.Value.X - diameter / 2,
            clickPos.Value.Y - diameter / 2, 0, 0);

        adornerLayer.Children.Add(ellipse);

        // 启动动画
        var scale = (ScaleTransform)ellipse.RenderTransform!;
        var startTicks = Environment.TickCount64;

        var timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        timer.Tick += (_, _) =>
        {
            var elapsed = Environment.TickCount64 - startTicks;
            var progress = Math.Min(1.0, elapsed / (double)RippleDuration.TotalMilliseconds);

            if (progress >= 1.0)
            {
                timer.Stop();
                adornerLayer.Children.Remove(ellipse);
                return;
            }

            // easeOutCubic 缓动
            var eased = 1.0 - Math.Pow(1.0 - progress, 3);

            // 透明度曲线: 0 → max(快,前 20%)→ 0(慢,后 80%)
            var opacity = progress < 0.2
                ? RippleOpacity * (progress / 0.2)
                : RippleOpacity * (1.0 - (progress - 0.2) / 0.8);

            scale.ScaleX = eased;
            scale.ScaleY = eased;
            ellipse.Opacity = opacity;
        };
        timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
    }
}
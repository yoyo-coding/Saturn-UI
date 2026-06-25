using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace SaturnUI.Controls;

/// <summary>
/// 继承 Button 的 M3 涟漪按钮,点击时从按压点向四周扩散的圆形涟漪动效
/// 使用 ScaleTransform 缩放 + 透明度衰减,GPU 加速渲染,流畅不卡顿
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

    private readonly List<RippleInfo> _activeRipples = new();

    private class RippleInfo
    {
        public Ellipse Element { get; set; } = null!;
        public Grid Container { get; set; } = null!;
        public double Diameter { get; set; }
        public long StartTicks { get; set; }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var position = e.GetCurrentPoint(this).Position;
        var adornerLayer = AdornerLayer.GetAdornerLayer(this);
        if (adornerLayer is null) return;

        CreateRipple(adornerLayer, position);
    }

    private void CreateRipple(AdornerLayer adornerLayer, Point position)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        // 涟漪直径 = 按钮最大边长 * 2.5
        var diameter = Math.Max(Bounds.Width, Bounds.Height) * 2.5;

        // 涟漪颜色
        var brush = RippleBrush ?? Foreground ?? new SolidColorBrush(Colors.White);

        // 使用 ScaleTransform 而非 Width/Height(触发渲染属性而不重新布局)
        var scaleTransform = new ScaleTransform(0, 0);
        var ellipse = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = brush,
            IsHitTestVisible = false,
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            RenderTransform = scaleTransform,
            Opacity = 0
        };

        // 容器 Grid
        var container = new Grid
        {
            Width = Bounds.Width,
            Height = Bounds.Height,
            IsHitTestVisible = false,
            ClipToBounds = true,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        container.Children.Add(ellipse);

        // 定位椭圆中心对准点击位置
        ellipse.Margin = new Thickness(position.X - diameter / 2, position.Y - diameter / 2, 0, 0);

        // 定位容器到按钮位置
        var buttonPos = this.TranslatePoint(new Point(0, 0), adornerLayer);
        if (buttonPos.HasValue)
        {
            container.Margin = new Thickness(buttonPos.Value.X, buttonPos.Value.Y, 0, 0);
        }

        // 添加到 AdornerLayer
        adornerLayer.Children.Add(container);

        var ripple = new RippleInfo
        {
            Element = ellipse,
            Container = container,
            Diameter = diameter,
            StartTicks = Environment.TickCount64
        };
        _activeRipples.Add(ripple);

        // 使用 60fps 定时器(每帧 ~16ms),ScaleTransform 不触发重新布局,性能高
        var timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) => AnimateRipple(ripple, scaleTransform, timer, adornerLayer);
        timer.Start();
    }

    private void AnimateRipple(RippleInfo ripple, ScaleTransform scale, DispatcherTimer timer, AdornerLayer adornerLayer)
    {
        var elapsed = Environment.TickCount64 - ripple.StartTicks;
        var progress = Math.Min(1.0, elapsed / (double)RippleDuration.TotalMilliseconds);

        if (progress >= 1.0)
        {
            timer.Stop();
            adornerLayer.Children.Remove(ripple.Container);
            _activeRipples.Remove(ripple);
            return;
        }

        // easeOutCubic 缓动曲线(让动画开始快速,结束平滑)
        var eased = 1.0 - Math.Pow(1.0 - progress, 3);

        // 透明度曲线: 0 -> max(快) -> 0(慢)
        // 前 20% 快速上升至峰值,后 80% 平滑衰减
        var opacity = progress < 0.2
            ? RippleOpacity * (progress / 0.2)
            : RippleOpacity * (1.0 - (progress - 0.2) / 0.8);

        scale.ScaleX = eased;
        scale.ScaleY = eased;
        ripple.Element.Opacity = opacity;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        var adornerLayer = AdornerLayer.GetAdornerLayer(this);
        foreach (var ripple in _activeRipples)
        {
            adornerLayer?.Children.Remove(ripple.Container);
        }
        _activeRipples.Clear();
    }
}
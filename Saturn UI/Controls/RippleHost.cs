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
/// - 涟漪 Ellipse 通过 AdornerLayer 渲染,包裹在 Border 容器中
/// - 容器继承按钮 CornerRadius,矩形遮罩与按钮形状一致
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

        // 涟漪 Ellipse: 中心对准点击位置
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
                buttonPosition.X - diameter / 2,
                buttonPosition.Y - diameter / 2, 0, 0)
        };

        // 容器: Border,继承按钮 CornerRadius,让裁剪形状与按钮一致
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

        // 容器在 AdornerLayer 坐标系中,定位到按钮位置
        var containerPos = this.TranslatePoint(new Point(0, 0), adornerLayer);
        if (containerPos.HasValue)
        {
            container.Margin = new Thickness(containerPos.Value.X, containerPos.Value.Y, 0, 0);
        }

        adornerLayer.Children.Add(container);

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
                adornerLayer.Children.Remove(container);
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
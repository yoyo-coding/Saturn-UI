using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace SaturnUI.Controls;

/// <summary>
/// 继承 Button 的 M3 涟漪按钮,点击时从按压点向四周扩散的圆形涟漪动效
/// </summary>
public class RippleButton : Button
{
    public static readonly StyledProperty<IBrush?> RippleBrushProperty =
        AvaloniaProperty.Register<RippleButton, IBrush?>(nameof(RippleBrush));

    public static readonly StyledProperty<double> RippleOpacityProperty =
        AvaloniaProperty.Register<RippleButton, double>(nameof(RippleOpacity), 0.16);

    public static readonly StyledProperty<TimeSpan> RippleDurationProperty =
        AvaloniaProperty.Register<RippleButton, TimeSpan>(nameof(RippleDuration),
            TimeSpan.FromMilliseconds(450));

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

    private Canvas? _rippleCanvas;
    private readonly DispatcherTimer _cleanupTimer;
    private readonly List<Ellipse> _activeRipples = new();

    public RippleButton()
    {
        ClipToBounds = true;
        _cleanupTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _cleanupTimer.Tick += (_, _) => CleanupRipples();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // 找到 ContentPresenter,把 Canvas 插入到它的父级 Panel 中
        var presenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        if (presenter?.Parent is not Panel host) return;

        _rippleCanvas = new Canvas
        {
            Name = "PART_RippleCanvas",
            IsHitTestVisible = false
        };
        host.Children.Add(_rippleCanvas);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var p = e.GetCurrentPoint(this);
        if (p.Properties.IsLeftButtonPressed && _rippleCanvas is not null)
        {
            SpawnRipple(p.Position);
        }
    }

    private void SpawnRipple(Point position)
    {
        if (_rippleCanvas is null) return;

        var diameter = Math.Max(Bounds.Width, Bounds.Height) * 2.2;
        var radius = diameter / 2.0;

        // 涟漪颜色:用户自定义 / 用 Foreground(主色)/ 兜底白色
        var brush = RippleBrush ?? Foreground ?? new SolidColorBrush(Colors.White);

        var ripple = new Ellipse
        {
            Width = 0,
            Height = 0,
            Fill = brush,
            Opacity = RippleOpacity,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(ripple, position.X - radius);
        Canvas.SetTop(ripple, position.Y - radius);
        _rippleCanvas.Children.Add(ripple);
        _activeRipples.Add(ripple);

        var frames = 30;
        var interval = RippleDuration.TotalMilliseconds / frames;
        var step = 0;

        var sizeTick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(interval) };
        sizeTick.Tick += (_, _) =>
        {
            step++;
            var t = (double)step / frames;
            var eased = 1.0 - Math.Pow(1.0 - t, 3); // easeOutCubic
            var cur = diameter * eased;
            ripple.Width = cur;
            ripple.Height = cur;
            if (step >= frames) sizeTick.Stop();
        };

        var fadeTick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(interval) };
        fadeTick.Tick += (_, _) =>
        {
            var t = (double)step / frames;
            ripple.Opacity = RippleOpacity * (1.0 - t);
            if (step >= frames) fadeTick.Stop();
        };

        sizeTick.Start();
        fadeTick.Start();

        if (!_cleanupTimer.IsEnabled) _cleanupTimer.Start();
    }

    private void CleanupRipples()
    {
        if (_rippleCanvas is null) return;
        var threshold = Math.Max(Bounds.Width, Bounds.Height) * 2.0;
        _activeRipples.RemoveAll(r =>
        {
            if (r.Width >= threshold)
            {
                _rippleCanvas.Children.Remove(r);
                return true;
            }
            return false;
        });
        if (_activeRipples.Count == 0) _cleanupTimer.Stop();
    }
}

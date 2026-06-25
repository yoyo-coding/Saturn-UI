using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace SaturnUI.Controls;

/// <summary>
/// 涟漪可选中项 - ItemsControl 风格的可选中列表项
///
/// 为什么需要这个类:
///   - ListBox/ListBoxItem 有内置的内部事件处理,会标记 Handled,拦截附加属性订阅
///   - Button 的 OnPointerPressed 也会被某些场景标记 Handled
///   - ContentControl 没有内置交互逻辑,可完美拦截 PointerPressed,完全控制行为
///
/// 用法 (XAML):
///   &lt;ItemsControl ItemsSource="{Binding Items}"&gt;
///     &lt;ItemsControl.ItemTemplate&gt;
///       &lt;DataTemplate&gt;
///         &lt;controls:RippleSelectableItem
///             IsSelected="{Binding IsSelected, Mode=TwoWay}"
///             Command="{Binding ...}"
///             CommandParameter="{Binding}"&gt;
///           &lt;TextBlock Text="{Binding Name}" /&gt;
///         &lt;/controls:RippleSelectableItem&gt;
///       &lt;/DataTemplate&gt;
///     &lt;/ItemsControl.ItemTemplate&gt;
///   &lt;/ItemsControl&gt;
/// </summary>
public class RippleSelectableItem : ContentControl
{
    // ===== Styled Properties =====

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<RippleSelectableItem, bool>(nameof(IsSelected));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<RippleSelectableItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<RippleSelectableItem, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<IBrush?> SelectedBackgroundProperty =
        AvaloniaProperty.Register<RippleSelectableItem, IBrush?>(nameof(SelectedBackground));

    public static readonly StyledProperty<IBrush?> HoverBackgroundProperty =
        AvaloniaProperty.Register<RippleSelectableItem, IBrush?>(nameof(HoverBackground));

    public static readonly StyledProperty<double> RippleOpacityProperty =
        AvaloniaProperty.Register<RippleSelectableItem, double>(nameof(RippleOpacity), 0.24);

    public static readonly StyledProperty<TimeSpan> RippleDurationProperty =
        AvaloniaProperty.Register<RippleSelectableItem, TimeSpan>(
            nameof(RippleDuration), TimeSpan.FromMilliseconds(550));

    public static readonly StyledProperty<IBrush?> RippleBrushProperty =
        AvaloniaProperty.Register<RippleSelectableItem, IBrush?>(nameof(RippleBrush));

    // ===== Static Constructor: 同步 :selected 伪类 =====

    static RippleSelectableItem()
    {
        // 关键: IsSelected 是普通 StyledProperty,不会自动触发 :selected 伪类
        // 必须显式同步,样式 Selector "controls|RippleSelectableItem:selected" 才能生效
        IsSelectedProperty.Changed.AddClassHandler<RippleSelectableItem>((item, e) =>
        {
            item.PseudoClasses.Set(":selected", e.NewValue is true);
        });
    }

    // ===== Property Accessors =====

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public IBrush? SelectedBackground
    {
        get => GetValue(SelectedBackgroundProperty);
        set => SetValue(SelectedBackgroundProperty, value);
    }

    public IBrush? HoverBackground
    {
        get => GetValue(HoverBackgroundProperty);
        set => SetValue(HoverBackgroundProperty, value);
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

    public IBrush? RippleBrush
    {
        get => GetValue(RippleBrushProperty);
        set => SetValue(RippleBrushProperty, value);
    }

    /// <summary>
    /// 属性变化时同步视觉状态(Background/Foreground)
    /// 不依赖 :selected 伪类,直接通过代码设置,确保跨主题/跨样式生效
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsSelectedProperty)
        {
            UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        if (IsSelected && SelectedBackground != null)
        {
            Background = SelectedBackground;
        }
        else
        {
            Background = Avalonia.Media.Brushes.Transparent;
        }
    }

    /// <summary>
    /// 拦截 PointerPressed - 触发涟漪 + 执行命令
    /// ContentControl 没有内置交互,可完美触发
    /// </summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // 1. 播放涟漪
            var position = e.GetCurrentPoint(this).Position;
            RippleAnimator.Play(this, position, RippleBrush, RippleOpacity, RippleDuration);

            // 2. 执行命令(切换选中)
            if (Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
            }

            // 3. 立即更新自己,不等 binding 回流(避免视觉延迟)
            IsSelected = true;
            PseudoClasses.Set(":selected", true);
        }
        base.OnPointerPressed(e);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateVisualState();
    }
}

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaturnUI.ViewModels;

namespace SaturnUI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is SettingsViewModel vm)
        {
            vm.OpenProviderConfigRequested += OnOpenProviderConfigRequested;
        }
    }

    private async void OnOpenProviderConfigRequested(object? sender, string provider)
    {
        var topWindow = TopLevel.GetTopLevel(this) as Window;
        if (topWindow == null) return;

        Window configWindow;

        if (provider == "本地")
        {
            configWindow = new LocalProviderWindow
            {
                DataContext = DataContext
            };
        }
        else
        {
            configWindow = new OnlineProviderWindow
            {
                DataContext = DataContext
            };
        }

        await configWindow.ShowDialog(topWindow);

        // 窗口关闭后刷新状态显示
        if (DataContext is SettingsViewModel vm)
        {
            vm.RefreshStatus();
        }
    }
}

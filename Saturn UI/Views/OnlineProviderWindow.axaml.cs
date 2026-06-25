using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaturnUI.ViewModels;

namespace SaturnUI.Views;

public partial class OnlineProviderWindow : Window
{
    public OnlineProviderWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SaveSettings();
        }
        Close();
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaturnUI.Models;
using SaturnUI.ViewModels;

namespace SaturnUI.Views;

public partial class SessionListView : UserControl
{
    public SessionListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SessionsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is Session session)
        {
            if (DataContext is SessionListViewModel vm)
            {
                vm.SelectSessionCommand.Execute(session);
            }
        }
    }
}

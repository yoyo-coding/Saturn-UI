using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
}

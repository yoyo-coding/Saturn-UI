using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SaturnUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;

    [ObservableProperty]
    private bool _isSessionPanelOpen = true;

    [ObservableProperty]
    private bool _isSettingsOpen;

    public ChatViewModel ChatViewModel { get; }
    public SessionListViewModel SessionListViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }

    public MainViewModel(ChatViewModel chatVm, SessionListViewModel sessionListVm, SettingsViewModel settingsVm)
    {
        ChatViewModel = chatVm;
        SessionListViewModel = sessionListVm;
        SettingsViewModel = settingsVm;
        _currentView = chatVm;

        SessionListViewModel.SessionSelected += (_, session) =>
        {
            ChatViewModel.LoadSession(session);
        };
    }

    [RelayCommand]
    private void ToggleSessionPanel()
    {
        IsSessionPanelOpen = !IsSessionPanelOpen;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void NewChat()
    {
        ChatViewModel.NewSession();
    }
}

using System.ComponentModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaturnUI.Models;

namespace SaturnUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ChatViewModel ChatViewModel { get; }
    public SessionListViewModel SessionListViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public CodingWorkspaceViewModel CodingWorkspaceViewModel { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SessionPanelWidth))]
    [NotifyPropertyChangedFor(nameof(IsSessionPanelVisible))]
    private bool _isSessionPanelOpen = true;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChatMode))]
    [NotifyPropertyChangedFor(nameof(IsCodingMode))]
    [NotifyPropertyChangedFor(nameof(CurrentModeTitle))]
    [NotifyPropertyChangedFor(nameof(CurrentWorkspace))]
    [NotifyPropertyChangedFor(nameof(ActiveStatusText))]
    [NotifyPropertyChangedFor(nameof(SessionPanelWidth))]
    [NotifyPropertyChangedFor(nameof(IsSessionPanelVisible))]
    private AppMode _currentAppMode = AppMode.Chat;

    public bool IsChatMode => CurrentAppMode == AppMode.Chat;

    public bool IsCodingMode => CurrentAppMode == AppMode.Coding;

    public string CurrentModeTitle => IsChatMode ? "聊天" : "编程";

    public ViewModelBase CurrentWorkspace => IsChatMode ? ChatViewModel : CodingWorkspaceViewModel;

    public GridLength SessionPanelWidth => IsChatMode && IsSessionPanelOpen
        ? new GridLength(292)
        : new GridLength(0);

    public bool IsSessionPanelVisible => IsChatMode && IsSessionPanelOpen;

    public string ActiveStatusText => IsChatMode ? ChatViewModel.StatusText : CodingWorkspaceViewModel.StatusText;

    public MainViewModel(
        ChatViewModel chatVm,
        SessionListViewModel sessionListVm,
        SettingsViewModel settingsVm,
        CodingWorkspaceViewModel codingVm)
    {
        ChatViewModel = chatVm;
        SessionListViewModel = sessionListVm;
        SettingsViewModel = settingsVm;
        CodingWorkspaceViewModel = codingVm;

        SessionListViewModel.SessionSelected += OnSessionSelected;
        ChatViewModel.SessionCreated += OnSessionCreated;
        ChatViewModel.PropertyChanged += OnChildPropertyChanged;
        CodingWorkspaceViewModel.PropertyChanged += OnChildPropertyChanged;
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChatViewModel.StatusText) or nameof(CodingWorkspaceViewModel.StatusText))
            OnPropertyChanged(nameof(ActiveStatusText));
    }

    private void OnSessionSelected(object? sender, Session session)
    {
        ChatViewModel.LoadSession(session);
        CurrentAppMode = AppMode.Chat;
        IsSettingsOpen = false;
    }

    private void OnSessionCreated(object? sender, Session session)
        => SessionListViewModel.AddOrRefreshSession(session);

    [RelayCommand]
    private void ToggleNavigationPanel()
    {
        if (IsChatMode)
        {
            IsSessionPanelOpen = !IsSessionPanelOpen;
        }
        else
        {
            CodingWorkspaceViewModel.ToggleExplorer();
        }
    }

    [RelayCommand]
    private void OpenSettings() => IsSettingsOpen = true;

    [RelayCommand]
    private void CloseSettings() => IsSettingsOpen = false;

    [RelayCommand]
    private void SwitchToChatMode()
    {
        CurrentAppMode = AppMode.Chat;
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void SwitchToCodingMode()
    {
        CurrentAppMode = AppMode.Coding;
        IsSettingsOpen = false;
    }

    protected override void DisposeCore()
    {
        SessionListViewModel.SessionSelected -= OnSessionSelected;
        ChatViewModel.SessionCreated -= OnSessionCreated;
        ChatViewModel.PropertyChanged -= OnChildPropertyChanged;
        CodingWorkspaceViewModel.PropertyChanged -= OnChildPropertyChanged;
        ChatViewModel.Dispose();
        SettingsViewModel.Dispose();
        CodingWorkspaceViewModel.Dispose();
    }
}


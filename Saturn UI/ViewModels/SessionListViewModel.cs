using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaturnUI.Models;
using SaturnUI.Services;

namespace SaturnUI.ViewModels;

/// <summary>
/// ?????????????????????????????
/// </summary>
public partial class SessionListViewModel : ViewModelBase
{
    private readonly LocalStorageService _storage;
    private System.Collections.Generic.List<Session> _allSessions = new();

    [ObservableProperty]
    private ObservableCollection<Session> _sessions = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Session? _selectedSession;

    public event EventHandler<Session>? SessionSelected;

    public SessionListViewModel(LocalStorageService storage)
    {
        _storage = storage;
        LoadSessions();
    }

    public void LoadSessions()
    {
        _allSessions = _storage.GetSessions();
        ApplyFilter();
    }

    public void AddOrRefreshSession(Session session, bool select = false)
    {
        var existing = _allSessions.FirstOrDefault(s => s.Id == session.Id);
        if (existing is null)
            _allSessions.Insert(0, session);
        else
        {
            existing.Title = session.Title;
            existing.UpdatedAt = session.UpdatedAt;
        }

        _allSessions = _allSessions.OrderByDescending(s => s.UpdatedAt).ToList();
        ApplyFilter();

        if (select)
            SelectSessionCommand.Execute(_allSessions.First(s => s.Id == session.Id));
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allSessions
            : _allSessions.Where(s => s.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        Sessions.Clear();
        foreach (var s in filtered)
        {
            s.IsSelected = SelectedSession?.Id == s.Id;
            Sessions.Add(s);
        }
    }

    [RelayCommand]
    private void SelectSession(Session? session)
    {
        if (session is null) return;

        foreach (var s in Sessions) s.IsSelected = false;
        session.IsSelected = true;
        SelectedSession = session;

        var full = _storage.GetSession(session.Id);
        if (full != null) SessionSelected?.Invoke(this, full);
    }

    [RelayCommand]
    private void DeleteSession(Session? session)
    {
        if (session is null) return;
        _storage.DeleteSession(session.Id);
        _allSessions.RemoveAll(s => s.Id == session.Id);
        Sessions.Remove(session);
        if (SelectedSession?.Id == session.Id)
            SelectedSession = null;
    }

    [RelayCommand]
    public void NewSession()
    {
        var session = new Session(AppConstants.DefaultSessionTitle) { UpdatedAt = DateTime.Now };
        _storage.SaveSession(session);
        AddOrRefreshSession(session, select: true);
    }
}

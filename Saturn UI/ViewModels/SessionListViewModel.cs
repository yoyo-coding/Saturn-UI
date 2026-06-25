using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaturnUI.Models;
using SaturnUI.Services;

namespace SaturnUI.ViewModels;

/// <summary>
/// 会话列表视图模型
///
/// 优化:
///   1. 搜索框使用 OnSearchTextChanged 实时过滤
///   2. Sessions 集合用 Clear+Add 增量更新,避免重置
///   3. SelectSession 命令设置 SelectedSession + 同步每个 Session 的 IsSelected
///      (避免 XAML 复杂 binding 表达式,直接用 Session.IsSelected 通知)
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

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allSessions
            : _allSessions.Where(s => s.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        // 增量更新
        Sessions.Clear();
        foreach (var s in filtered)
        {
            s.IsSelected = ReferenceEquals(s, SelectedSession);
            Sessions.Add(s);
        }
    }

    [RelayCommand]
    private void SelectSession(Session? session)
    {
        if (session is null) return;

        // 1. 更新每个 Session 的 IsSelected(直接属性通知,简单可靠)
        foreach (var s in Sessions) s.IsSelected = false;
        session.IsSelected = true;

        // 2. 同步 SelectedSession 供其他 ViewModel 使用
        SelectedSession = session;

        // 3. 加载完整 session 并通知监听者
        var full = _storage.GetSession(session.Id);
        if (full != null) SessionSelected?.Invoke(this, full);
    }

    [RelayCommand]
    private void DeleteSession(Session? session)
    {
        if (session is null) return;
        _storage.DeleteSession(session.Id);
        _allSessions.Remove(session);
        Sessions.Remove(session);
        if (SelectedSession?.Id == session.Id)
        {
            SelectedSession = null;
        }
    }

    [RelayCommand]
    private void NewSession()
    {
        var session = new Session("新会话");
        _storage.SaveSession(session);
        _allSessions.Insert(0, session);
        Sessions.Insert(0, session);
        SelectSession(session);
    }
}

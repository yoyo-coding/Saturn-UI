using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaturnUI.Models;
using SaturnUI.Services;

namespace SaturnUI.ViewModels;

/// <summary>
/// 聊天视图模型
///
/// 优化:
///   1. 继承 ViewModelBase,统一 IsBusy/StatusText/ErrorMessage
///   2. 消息流式更新使用 AppendContent(string token) 替代 += ,
///      减少属性变更通知次数,提升渲染性能
///   3. Stop 命令通过 CancellationToken 统一控制
///   4. 移除冗余 _statusText / _isBusy / _errorMessage(已迁移到基类)
/// </summary>
public partial class ChatViewModel : ViewModelBase
{
    private readonly ChatService _chatService;
    private readonly LocalStorageService _storage;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private ObservableCollection<Message> _messages = new();

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private Session? _currentSession;

    [ObservableProperty]
    private string? _pendingAttachmentPath;

    [ObservableProperty]
    private string? _pendingAttachmentName;

    public ChatViewModel(ChatService chatService, LocalStorageService storage)
    {
        _chatService = chatService;
        _storage = storage;
    }

    public void LoadSession(Session session)
    {
        CurrentSession = session;
        // 增量更新 ObservableCollection,避免整体重置
        Messages.Clear();
        foreach (var msg in session.Messages)
            Messages.Add(msg);
        StatusText = $"会话: {session.Title}";
    }

    [RelayCommand]
    public void NewSession()
    {
        _cts?.Cancel();
        var session = new Session("新会话");
        _storage.SaveSession(session);
        LoadSession(session);
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || IsBusy) return;

        var userText = InputText.Trim();
        InputText = string.Empty;

        if (CurrentSession == null) NewSession();

        var sessionId = CurrentSession!.Id;
        var userMsg = new Message(MessageRole.User, userText)
        {
            SessionId = sessionId,
            AttachmentPath = PendingAttachmentPath,
            AttachmentName = PendingAttachmentName,
            HasAttachment = !string.IsNullOrEmpty(PendingAttachmentPath)
        };
        Messages.Add(userMsg);
        _storage.SaveMessage(userMsg);
        PendingAttachmentPath = null;
        PendingAttachmentName = null;

        var aiMsg = new Message(MessageRole.Assistant, "")
        {
            SessionId = sessionId,
            IsStreaming = true
        };
        Messages.Add(aiMsg);

        IsBusy = true;
        StatusText = "AI 正在思考...";
        _cts = new CancellationTokenSource();

        try
        {
            await foreach (var token in _chatService.SendMessageStreamAsync(
                userText, sessionId, _cts.Token))
            {
                if (token.StartsWith("[ERROR]"))
                {
                    aiMsg.IsError = true;
                    aiMsg.ErrorMessage = token[7..];
                    aiMsg.IsStreaming = false;
                    StatusText = "出错";
                    break;
                }

                aiMsg.AppendContent(token);
            }

            aiMsg.IsStreaming = false;
            _storage.SaveMessage(aiMsg);

            if (!aiMsg.IsError)
            {
                StatusText = "完成";
                if (CurrentSession!.Title == "新会话" && Messages.Count >= 2)
                {
                    var title = userText.Length > 20 ? userText[..20] + "..." : userText;
                    CurrentSession.Title = title;
                    _storage.UpdateSessionTitle(CurrentSession.Id, title);
                }
            }

            CurrentSession.UpdatedAt = DateTime.Now;
            _storage.SaveSession(CurrentSession);
        }
        catch (OperationCanceledException)
        {
            aiMsg.IsStreaming = false;
            StatusText = "已取消";
        }
        catch (Exception ex)
        {
            aiMsg.IsError = true;
            aiMsg.ErrorMessage = ex.Message;
            aiMsg.IsStreaming = false;
            StatusText = "错误";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void StopGeneration() => _cts?.Cancel();

    [RelayCommand]
    private void ClearAttachment()
    {
        PendingAttachmentPath = null;
        PendingAttachmentName = null;
    }

    public void AttachFiles(string[] paths)
    {
        if (paths.Length == 0) return;
        PendingAttachmentPath = paths[0];
        PendingAttachmentName = System.IO.Path.GetFileName(paths[0]);
    }

    protected override void DisposeCore()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}

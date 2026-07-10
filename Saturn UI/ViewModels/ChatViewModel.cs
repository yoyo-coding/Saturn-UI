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
/// ?????????????????????????????
/// </summary>
public partial class ChatViewModel : ViewModelBase
{
    private readonly ChatService _chatService;
    private readonly LocalStorageService _storage;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private ObservableCollection<Message> _messages = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private Session? _currentSession;

    [ObservableProperty]
    private string? _pendingAttachmentPath;

    [ObservableProperty]
    private string? _pendingAttachmentName;

    public event EventHandler<Session>? SessionCreated;

    public ChatViewModel(ChatService chatService, LocalStorageService storage)
    {
        _chatService = chatService;
        _storage = storage;
        StatusText = "就绪";
    }

    public void LoadSession(Session session)
    {
        CurrentSession = session;
        Messages.Clear();
        foreach (var msg in session.Messages)
            Messages.Add(msg);
        StatusText = $"已加载：{session.Title}";
        ClearError();
    }

    [RelayCommand]
    public void NewSession()
    {
        _cts?.Cancel();
        var session = new Session(AppConstants.DefaultSessionTitle);
        session.UpdatedAt = DateTime.Now;
        _storage.SaveSession(session);
        LoadSession(session);
        SessionCreated?.Invoke(this, session);
    }

    private bool CanSendMessage() => !IsBusy && !string.IsNullOrWhiteSpace(InputText);

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync()
    {
        if (!CanSendMessage()) return;

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

        var aiMsg = new Message(MessageRole.Assistant, string.Empty)
        {
            SessionId = sessionId,
            IsStreaming = true
        };
        Messages.Add(aiMsg);

        IsBusy = true;
        SendMessageCommand.NotifyCanExecuteChanged();
        StatusText = "AI 正在生成...";
        ClearError();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        try
        {
            await foreach (var token in _chatService.SendMessageStreamAsync(userText, sessionId, _cts.Token))
            {
                if (token.StartsWith("[ERROR]", StringComparison.Ordinal))
                {
                    aiMsg.IsError = true;
                    aiMsg.ErrorMessage = token[7..];
                    RaiseError(aiMsg.ErrorMessage);
                    break;
                }

                aiMsg.AppendContent(token);
            }

            aiMsg.CompleteStreaming();
            _storage.SaveMessage(aiMsg);

            if (!aiMsg.IsError)
            {
                StatusText = "完成";
                TryPromoteSessionTitle(userText);
            }

            CurrentSession!.UpdatedAt = DateTime.Now;
            _storage.SaveSession(CurrentSession);
        }
        catch (OperationCanceledException)
        {
            aiMsg.CompleteStreaming();
            StatusText = "已取消";
        }
        catch (Exception ex)
        {
            aiMsg.CompleteStreaming();
            aiMsg.IsError = true;
            aiMsg.ErrorMessage = ex.Message;
            RaiseError(ex.Message);
            _storage.SaveMessage(aiMsg);
        }
        finally
        {
            IsBusy = false;
            SendMessageCommand.NotifyCanExecuteChanged();
        }
    }

    private void TryPromoteSessionTitle(string userText)
    {
        if (CurrentSession is null || CurrentSession.Title != AppConstants.DefaultSessionTitle || Messages.Count < 2)
            return;

        var normalized = userText.ReplaceLineEndings(" ").Trim();
        var title = normalized.Length > 20 ? normalized[..20] + "..." : normalized;
        CurrentSession.Title = string.IsNullOrWhiteSpace(title) ? AppConstants.DefaultSessionTitle : title;
        _storage.UpdateSessionTitle(CurrentSession.Id, CurrentSession.Title);
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

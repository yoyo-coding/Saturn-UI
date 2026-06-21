using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaturnUI.Models;
using SaturnUI.Services;

namespace SaturnUI.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    private readonly ChatService _chatService;
    private readonly LocalStorageService _storage;

    [ObservableProperty]
    private ObservableCollection<Message> _messages = new();

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private Session? _currentSession;

    [ObservableProperty]
    private string? _pendingAttachmentPath;

    [ObservableProperty]
    private string? _pendingAttachmentName;

    private CancellationTokenSource? _cts;

    public ChatViewModel(ChatService chatService, LocalStorageService storage)
    {
        _chatService = chatService;
        _storage = storage;
    }

    public void LoadSession(Session session)
    {
        CurrentSession = session;
        Messages = new ObservableCollection<Message>(session.Messages);
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
        if (string.IsNullOrWhiteSpace(InputText) || IsBusy)
            return;

        var userText = InputText.Trim();
        InputText = string.Empty;

        if (CurrentSession == null)
        {
            NewSession();
        }

        var userMsg = new Message(MessageRole.User, userText)
        {
            SessionId = CurrentSession!.Id,
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
            SessionId = CurrentSession.Id,
            IsStreaming = true
        };
        Messages.Add(aiMsg);

        IsBusy = true;
        StatusText = "AI 正在思考...";
        _cts = new CancellationTokenSource();

        try
        {
            await foreach (var token in _chatService.SendMessageStreamAsync(
                userText, CurrentSession.Id, _cts.Token))
            {
                if (token.StartsWith("[ERROR]"))
                {
                    aiMsg.IsError = true;
                    aiMsg.ErrorMessage = token.Substring(7);
                    aiMsg.IsStreaming = false;
                    StatusText = "出错";
                    break;
                }

                aiMsg.Content += token;
            }

            aiMsg.IsStreaming = false;
            _storage.SaveMessage(aiMsg);

            if (!aiMsg.IsError)
            {
                StatusText = "完成";
                if (CurrentSession.Title == "新会话" && Messages.Count >= 2)
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
    private void StopGeneration()
    {
        _cts?.Cancel();
    }

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
}

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaturnUI.Models;
using SaturnUI.Services.Coding;

namespace SaturnUI.ViewModels;

public partial class CodingWorkspaceViewModel : ViewModelBase
{
    private readonly CodeFileService _fileService;
    private readonly CodeAssistantService _assistantService;
    private CancellationTokenSource? _completionCts;
    private CancellationTokenSource? _assistantCts;

    public ObservableCollection<CodeWorkspaceNode> WorkspaceRoots { get; } = new();
    public ObservableCollection<CodeWorkspaceNode> VisibleWorkspaceNodes { get; } = new();
    public ObservableCollection<CodeAssistantMessage> AssistantMessages { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDocument))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private CodeDocument? _activeDocument;

    [ObservableProperty]
    private string _workspaceTitle = "Coding Mode";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGhostSuggestion))]
    private string _ghostSuggestion = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AskAssistantCommand))]
    private string _assistantInput = string.Empty;

    [ObservableProperty]
    private string? _currentSelectionText;

    [ObservableProperty]
    private bool _isExplorerOpen = true;

    public bool HasDocument => ActiveDocument is not null;
    public bool HasGhostSuggestion => !string.IsNullOrWhiteSpace(GhostSuggestion);

    public CodingWorkspaceViewModel(CodeFileService fileService, CodeAssistantService assistantService)
    {
        _fileService = fileService;
        _assistantService = assistantService;
        StatusText = "Open a file or folder to start coding";
    }

    public void ToggleExplorer() => IsExplorerOpen = !IsExplorerOpen;

    public void UpdateDocumentText(string text)
    {
        if (ActiveDocument is null)
            return;

        if (!string.Equals(ActiveDocument.Text, text, StringComparison.Ordinal))
            ActiveDocument.Text = text;

        WorkspaceTitle = ActiveDocument.DisplayName;
        SaveCommand.NotifyCanExecuteChanged();
    }

    public void UpdateSelectionText(string? selection)
        => CurrentSelectionText = string.IsNullOrWhiteSpace(selection) ? null : selection;

    public Task OpenFileAsync(string filePath) => OpenFileCoreAsync(filePath, clearWorkspace: true);

    private async Task OpenFileCoreAsync(string filePath, bool clearWorkspace)
    {
        await RunBusyAsync(async () =>
        {
            ActiveDocument = await _fileService.OpenFileAsync(filePath);
            WorkspaceTitle = ActiveDocument.DisplayName;
            if (clearWorkspace)
            {
                WorkspaceRoots.Clear();
                VisibleWorkspaceNodes.Clear();
            }

            GhostSuggestion = string.Empty;
            StatusText = $"Opened {ActiveDocument.FileName}";
        }, "Opening file...", "File opened");
    }

    public async Task OpenFolderAsync(string folderPath)
    {
        await RunBusyAsync(async () =>
        {
            var root = await _fileService.BuildWorkspaceTreeAsync(folderPath);
            WorkspaceRoots.Clear();
            WorkspaceRoots.Add(root);
            RebuildVisibleWorkspaceNodes();
            WorkspaceTitle = root.Name;
            ActiveDocument = null;
            GhostSuggestion = string.Empty;
            StatusText = $"Opened folder {root.Name}";
        }, "Opening folder...", "Folder opened");
    }

    public async Task OpenNodeAsync(CodeWorkspaceNode? node)
    {
        if (node is null || node.IsDirectory)
            return;

        await OpenFileCoreAsync(node.Path, clearWorkspace: false);
    }

    public async Task SaveCurrentAsAsync(string newFilePath)
    {
        if (ActiveDocument is null)
            return;

        await RunBusyAsync(async () =>
        {
            ActiveDocument = await _fileService.SaveFileAsAsync(ActiveDocument, newFilePath);
            WorkspaceTitle = ActiveDocument.DisplayName;
            StatusText = $"Saved as {Path.GetFileName(newFilePath)}";
        }, "Saving as...", "Saved as");
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (ActiveDocument is null)
            return;

        await RunBusyAsync(async () =>
        {
            await _fileService.SaveFileAsync(ActiveDocument);
            WorkspaceTitle = ActiveDocument.DisplayName;
            StatusText = $"Saved {ActiveDocument.FileName}";
        }, "Saving...", "Saved");
    }

    private bool CanSave() => ActiveDocument?.IsDirty == true;

    [RelayCommand]
    private void CloseDocument()
    {
        ActiveDocument = null;
        GhostSuggestion = string.Empty;
        WorkspaceTitle = "Coding Mode";
        StatusText = "Document closed";
    }

    [RelayCommand]
    private void ToggleExplorerPanel() => ToggleExplorer();

    [RelayCommand(CanExecute = nameof(CanAskAssistant))]
    private async Task AskAssistantAsync()
    {
        var question = AssistantInput.Trim();
        if (string.IsNullOrWhiteSpace(question))
            return;

        AssistantInput = string.Empty;
        AssistantMessages.Add(new CodeAssistantMessage("user", question));
        var assistantMessage = new CodeAssistantMessage("assistant", string.Empty);
        AssistantMessages.Add(assistantMessage);

        _assistantCts?.Cancel();
        _assistantCts?.Dispose();
        _assistantCts = new CancellationTokenSource();

        try
        {
            IsBusy = true;
            StatusText = "AI is analyzing...";
            var summary = _fileService.BuildTreeSummary(WorkspaceRoots);
            assistantMessage.Content = await _assistantService.AskAsync(question, ActiveDocument, CurrentSelectionText, summary, _assistantCts.Token);
            StatusText = "AI response ready";
        }
        catch (OperationCanceledException)
        {
            assistantMessage.Content = "Canceled.";
            StatusText = "Canceled";
        }
        catch (Exception ex)
        {
            assistantMessage.Content = ex.Message;
            RaiseError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAskAssistant() => !string.IsNullOrWhiteSpace(AssistantInput);

    public async Task RequestInlineSuggestionAsync(int caretOffset)
    {
        if (ActiveDocument is null || ActiveDocument.Language == ProgrammingLanguage.PlainText)
        {
            GhostSuggestion = string.Empty;
            return;
        }

        _completionCts?.Cancel();
        _completionCts?.Dispose();
        _completionCts = new CancellationTokenSource();
        var token = _completionCts.Token;

        try
        {
            StatusText = "Requesting completion...";
            var suggestion = await _assistantService.GetCompletionAsync(ActiveDocument, caretOffset, token);
            if (!token.IsCancellationRequested)
            {
                GhostSuggestion = suggestion;
                StatusText = string.IsNullOrWhiteSpace(suggestion) ? "No completion" : "Completion ready";
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            GhostSuggestion = string.Empty;
            StatusText = ex.Message;
        }
    }

    public void CancelInlineSuggestion()
    {
        _completionCts?.Cancel();
        GhostSuggestion = string.Empty;
    }

    private void RebuildVisibleWorkspaceNodes()
    {
        VisibleWorkspaceNodes.Clear();
        foreach (var root in WorkspaceRoots)
            Append(root);

        void Append(CodeWorkspaceNode node)
        {
            VisibleWorkspaceNodes.Add(node);
            foreach (var child in node.Children)
                Append(child);
        }
    }

    partial void OnActiveDocumentChanged(CodeDocument? value)
    {
        SaveCommand.NotifyCanExecuteChanged();
        GhostSuggestion = string.Empty;
        WorkspaceTitle = value?.DisplayName ?? "Coding Mode";
    }

    partial void OnAssistantInputChanged(string value) => AskAssistantCommand.NotifyCanExecuteChanged();

    protected override void DisposeCore()
    {
        _completionCts?.Cancel();
        _completionCts?.Dispose();
        _assistantCts?.Cancel();
        _assistantCts?.Dispose();
    }
}


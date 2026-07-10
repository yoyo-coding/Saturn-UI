using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit;
using SaturnUI.Models;
using SaturnUI.Services.Coding;
using SaturnUI.ViewModels;

namespace SaturnUI.Views;

public partial class CodingWorkspaceView : UserControl
{
    private readonly DispatcherTimer _completionTimer;
    private readonly CodeHighlightingService _highlightingService = new();
    private TextEditor? _editor;
    private INotifyPropertyChanged? _currentViewModel;
    private bool _isUpdatingEditor;

    public CodingWorkspaceView()
    {
        InitializeComponent();
        _completionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(600),
        };
        _completionTimer.Tick += OnCompletionTimerTick;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _editor = this.FindControl<TextEditor>("Editor");
        ConfigureEditor();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_currentViewModel is not null)
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;

        base.OnDataContextChanged(e);

        _currentViewModel = DataContext as INotifyPropertyChanged;
        if (_currentViewModel is not null)
            _currentViewModel.PropertyChanged += OnViewModelPropertyChanged;

        SyncDocumentToEditor();
    }

    private void ConfigureEditor()
    {
        if (_editor is null)
            return;

        _editor.TextArea.SelectionChanged += (_, _) => UpdateSelection();
        _editor.TextArea.Caret.PositionChanged += (_, _) =>
        {
            if (DataContext is CodingWorkspaceViewModel vm)
                vm.CancelInlineSuggestion();
            UpdateSelection();
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CodingWorkspaceViewModel.ActiveDocument))
            Dispatcher.UIThread.Post(SyncDocumentToEditor);
    }

    private void SyncDocumentToEditor()
    {
        if (_editor is null || DataContext is not CodingWorkspaceViewModel vm)
            return;

        _isUpdatingEditor = true;
        _editor.Text = vm.ActiveDocument?.Text ?? string.Empty;
        _editor.IsReadOnly = vm.ActiveDocument is null;
        _editor.Options.ShowTabs = true;
        _editor.Options.ConvertTabsToSpaces = true;
        _editor.Options.IndentationSize = 4;
        _editor.Options.EnableRectangularSelection = true;
        _editor.Options.HighlightCurrentLine = true;

        var language = vm.ActiveDocument?.Language ?? ProgrammingLanguage.PlainText;
        _highlightingService.Apply(_editor, language);
        _isUpdatingEditor = false;
    }

    private async void OnOpenFileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not CodingWorkspaceViewModel vm || TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open code file",
            AllowMultiple = false,
        });

        if (files.Count > 0)
            await vm.OpenFileAsync(files[0].Path.LocalPath);
    }

    private async void OnOpenFolderClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not CodingWorkspaceViewModel vm || TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open code folder",
            AllowMultiple = false,
        });

        if (folders.Count > 0)
            await vm.OpenFolderAsync(folders[0].Path.LocalPath);
    }

    private async void OnSaveAsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not CodingWorkspaceViewModel vm || TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        var currentName = vm.ActiveDocument?.FileName ?? "untitled.txt";
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save code file as",
            SuggestedFileName = currentName,
        });

        if (file != null)
            await vm.SaveCurrentAsAsync(file.Path.LocalPath);
    }

    private async void OnTreeDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not CodingWorkspaceViewModel vm || sender is not ListBox tree)
            return;

        if (tree.SelectedItem is CodeWorkspaceNode node)
            await vm.OpenNodeAsync(node);
    }

    private async void OnTreeSelectedItemChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not CodingWorkspaceViewModel vm || e.AddedItems.Count == 0)
            return;

        if (e.AddedItems[0] is CodeWorkspaceNode { IsDirectory: false } node)
            await vm.OpenNodeAsync(node);
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingEditor || _editor is null || DataContext is not CodingWorkspaceViewModel vm)
            return;

        vm.UpdateDocumentText(_editor.Text ?? string.Empty);
        vm.CancelInlineSuggestion();
        _completionTimer.Stop();

        if (vm.ActiveDocument is { Language: not ProgrammingLanguage.PlainText })
            _completionTimer.Start();
    }

    private async void OnCompletionTimerTick(object? sender, EventArgs e)
    {
        _completionTimer.Stop();
        if (_editor is null || DataContext is not CodingWorkspaceViewModel vm)
            return;

        vm.UpdateDocumentText(_editor.Text ?? string.Empty);
        await vm.RequestInlineSuggestionAsync(_editor.CaretOffset);
    }

    private async void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (_editor is null || DataContext is not CodingWorkspaceViewModel vm)
            return;

        if (e.Key == Key.Tab && vm.HasGhostSuggestion)
        {
            var suggestion = vm.GhostSuggestion;
            vm.CancelInlineSuggestion();
            _editor.Document.Insert(_editor.CaretOffset, suggestion);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && vm.HasGhostSuggestion)
        {
            vm.CancelInlineSuggestion();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            vm.UpdateDocumentText(_editor.Text ?? string.Empty);
            await vm.RequestInlineSuggestionAsync(_editor.CaretOffset);
            e.Handled = true;
        }
    }

    private void OnAssistantInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not CodingWorkspaceViewModel vm)
            return;

        if (vm.AskAssistantCommand.CanExecute(null))
        {
            vm.AskAssistantCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void UpdateSelection()
    {
        if (_editor is null || DataContext is not CodingWorkspaceViewModel vm)
            return;

        var selected = _editor.TextArea.Selection.GetText();
        vm.UpdateSelectionText(string.IsNullOrWhiteSpace(selected) ? null : selected);
    }
}

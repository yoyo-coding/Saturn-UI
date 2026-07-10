using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SaturnUI.Services;
using SaturnUI.ViewModels;

namespace SaturnUI.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.RemoveHandler(InputElement.KeyDownEvent, OnPreviewKeyDown);
        }
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
        {
            TryPasteImage();
        }
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox || e.Key != Key.Enter)
            return;

        e.Handled = true;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            InsertLineBreak(textBox);
            return;
        }

        if (DataContext is not ChatViewModel vm)
            return;

        // Ensure the view model sees the latest text before the command clears it.
        vm.InputText = textBox.Text ?? string.Empty;

        if (vm.SendMessageCommand.CanExecute(null))
            vm.SendMessageCommand.Execute(null);
    }

    private static void InsertLineBreak(TextBox textBox)
    {
        var text = textBox.Text ?? string.Empty;
        var start = Math.Clamp(Math.Min(textBox.SelectionStart, textBox.SelectionEnd), 0, text.Length);
        var end = Math.Clamp(Math.Max(textBox.SelectionStart, textBox.SelectionEnd), 0, text.Length);
        const string lineBreak = "\n";

        textBox.Text = text[..start] + lineBreak + text[end..];
        textBox.CaretIndex = start + lineBreak.Length;
        textBox.SelectionStart = textBox.CaretIndex;
        textBox.SelectionEnd = textBox.CaretIndex;
    }

    private async void TryPasteImage()
    {
        if (TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        var clipboard = topLevel.Clipboard;
        if (clipboard == null)
            return;

        // Try to get image data directly
        var bitmap = await ClipboardExtensions.TryGetBitmapAsync(clipboard);
        if (bitmap != null)
        {
            SaveAndAttachImage(bitmap);
            return;
        }

        // Try file drop from clipboard
        var storageItems = await ClipboardExtensions.TryGetFilesAsync(clipboard);
        if (storageItems != null)
        {
            var paths = storageItems.Select(f => f.Path.LocalPath).ToArray();
            if (paths.Length > 0 && DataContext is ChatViewModel vm)
            {
                vm.AttachFiles(paths);
            }
        }
    }

    private void SaveAndAttachImage(Bitmap bitmap)
    {
        try
        {
            var dir = Path.Combine(AppDataPaths.ResolveDataDirectory(), "Images");
            Directory.CreateDirectory(dir);

            var fileName = $"pasted_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var filePath = Path.Combine(dir, fileName);

            bitmap.Save(filePath);

            if (DataContext is ChatViewModel vm)
            {
                vm.AttachFiles(new[] { filePath });
            }
        }
        catch { /* ignore paste errors */ }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Contains(DataFormat.File))
            return;

        var files = e.DataTransfer.TryGetFiles();
        if (files == null)
            return;

        var paths = files.Select(f => f.Path.LocalPath).ToArray();
        if (paths.Length > 0 && DataContext is ChatViewModel vm)
        {
            vm.AttachFiles(paths);
        }
    }
}



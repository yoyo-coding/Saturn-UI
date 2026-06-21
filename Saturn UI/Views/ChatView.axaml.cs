using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
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

    private async void TryPasteImage()
    {
        if (TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        var clipboard = topLevel.Clipboard;
        if (clipboard == null)
            return;

        var formats = await clipboard.GetFormatsAsync();

        // Try to get image data directly
        if (formats.Contains("image/png") || formats.Contains("image/bmp") || formats.Contains("image/jpeg"))
        {
            var bitmap = await clipboard.GetDataAsync("image/png") as Bitmap
                ?? await clipboard.GetDataAsync("image/bmp") as Bitmap
                ?? await clipboard.GetDataAsync("image/jpeg") as Bitmap;

            if (bitmap != null)
            {
                SaveAndAttachImage(bitmap);
                return;
            }
        }

        // Try file drop from clipboard
        var files = await clipboard.GetDataAsync(DataFormats.Files);
        if (files is IEnumerable<IStorageItem> storageItems)
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
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "SaturnUI", "Images");
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
        if (e.Data.Contains(DataFormats.Files))
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
        if (!e.Data.Contains(DataFormats.Files))
            return;

        var files = e.Data.GetFiles();
        if (files == null)
            return;

        var paths = files.Select(f => f.Path.LocalPath).ToArray();
        if (paths.Length > 0 && DataContext is ChatViewModel vm)
        {
            vm.AttachFiles(paths);
        }
    }
}

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SaturnUI.Models;

public partial class CodeWorkspaceNode : ObservableObject
{
    public CodeWorkspaceNode(string name, string path, bool isDirectory)
    {
        Name = name;
        Path = path;
        IsDirectory = isDirectory;
    }

    public string Name { get; }

    public string Path { get; }

    public bool IsDirectory { get; }

    public ObservableCollection<CodeWorkspaceNode> Children { get; } = new();

    [ObservableProperty]
    private bool _isExpanded;

    public string IconKind => IsDirectory ? "Folder" : "FileCode";

    public string TreeDisplayName => $"{(IsDirectory ? "?" : "  ")} {Name}";

    public string RelativePath(string basePath)
    {
        try
        {
            return Path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)
                ? System.IO.Path.GetRelativePath(basePath, Path)
                : Path;
        }
        catch
        {
            return Path;
        }
    }
}

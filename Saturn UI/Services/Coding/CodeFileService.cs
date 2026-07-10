using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SaturnUI.Models;

namespace SaturnUI.Services.Coding;

public sealed class CodeFileService
{
    public const long MaxFileBytes = 2L * 1024 * 1024;

    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".vs",
        ".idea",
        "bin",
        "obj",
        "node_modules",
        ".cache",
    };

    private readonly CodeLanguageService _languageService;

    public CodeFileService(CodeLanguageService languageService)
    {
        _languageService = languageService;
    }

    public async Task<CodeDocument> OpenFileAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found.", filePath);

        var info = new FileInfo(filePath);
        if (info.Length > MaxFileBytes)
            throw new InvalidOperationException($"File is larger than {MaxFileBytes / 1024 / 1024} MB and was not opened.");

        if (await LooksBinaryAsync(filePath, ct).ConfigureAwait(false))
            throw new InvalidOperationException("Binary files are not supported in coding mode yet.");

        var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8, ct).ConfigureAwait(false);
        return new CodeDocument(filePath, text, _languageService.DetectLanguage(filePath));
    }

    public async Task<CodeDocument> SaveFileAsync(CodeDocument document, CancellationToken ct = default)
    {
        await File.WriteAllTextAsync(document.FilePath, document.Text, Encoding.UTF8, ct).ConfigureAwait(false);
        document.MarkSaved(document.Text);
        return document;
    }

    public async Task<CodeDocument> SaveFileAsAsync(CodeDocument document, string newFilePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newFilePath))
            throw new ArgumentException("A destination file path is required.", nameof(newFilePath));

        await File.WriteAllTextAsync(newFilePath, document.Text, Encoding.UTF8, ct).ConfigureAwait(false);
        return new CodeDocument(newFilePath, document.Text, _languageService.DetectLanguage(newFilePath));
    }

    public async Task<CodeWorkspaceNode> BuildWorkspaceTreeAsync(string folderPath, CancellationToken ct = default)
        => await Task.Run(() => BuildWorkspaceTree(folderPath, ct), ct).ConfigureAwait(false);

    public CodeWorkspaceNode BuildWorkspaceTree(string folderPath, CancellationToken ct = default)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException(folderPath);

        var rootName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(rootName))
            rootName = folderPath;

        var root = new CodeWorkspaceNode(rootName, folderPath, true)
        {
            IsExpanded = true,
        };

        FillChildren(root, ct);
        return root;
    }

    public string BuildTreeSummary(IEnumerable<CodeWorkspaceNode> roots, int maxLines = 160)
    {
        var lines = new List<string>();
        foreach (var root in roots)
            AppendNode(root, 0);

        return string.Join(Environment.NewLine, lines.Take(maxLines));

        void AppendNode(CodeWorkspaceNode node, int depth)
        {
            if (lines.Count >= maxLines)
                return;

            lines.Add($"{new string(' ', depth * 2)}{(node.IsDirectory ? "[D]" : "[F]")} {node.Name}");
            foreach (var child in node.Children)
                AppendNode(child, depth + 1);
        }
    }

    public static bool IsDirectoryExcluded(string path)
    {
        var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return ExcludedDirectories.Contains(name);
    }

    private void FillChildren(CodeWorkspaceNode parent, CancellationToken ct)
    {
        IEnumerable<string> directories = Array.Empty<string>();
        IEnumerable<string> files = Array.Empty<string>();

        try
        {
            directories = Directory.EnumerateDirectories(parent.Path)
                .Where(directory => !IsDirectoryExcluded(directory))
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
            files = Directory.EnumerateFiles(parent.Path)
                .Where(IsLikelyTextOrCodeFile)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return;
        }

        foreach (var directory in directories)
        {
            ct.ThrowIfCancellationRequested();
            var child = new CodeWorkspaceNode(Path.GetFileName(directory), directory, true);
            parent.Children.Add(child);
            FillChildren(child, ct);
        }

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            parent.Children.Add(new CodeWorkspaceNode(Path.GetFileName(file), file, false));
        }
    }

    private bool IsLikelyTextOrCodeFile(string filePath)
    {
        if (_languageService.IsSupportedCodeFile(filePath))
            return true;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".txt" or ".md" or ".json" or ".xml" or ".css" or ".js" or ".ts" or ".cs" or ".xaml" or ".yaml" or ".yml";
    }

    private static async Task<bool> LooksBinaryAsync(string filePath, CancellationToken ct)
    {
        var length = (int)Math.Min(4096, new FileInfo(filePath).Length);
        var buffer = new byte[length];
        await using var stream = File.OpenRead(filePath);
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
        return buffer.Take(read).Any(b => b == 0);
    }
}

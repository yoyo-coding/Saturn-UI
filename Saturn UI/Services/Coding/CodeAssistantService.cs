using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SaturnUI.Models;

namespace SaturnUI.Services.Coding;

public sealed class CodeAssistantService
{
    private const int PrefixLimit = 3000;
    private const int SuffixLimit = 1200;
    private const int FileContextLimit = 10_000;
    private readonly ChatService _chatService;

    public CodeAssistantService(ChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<string> GetCompletionAsync(CodeDocument document, int caretOffset, CancellationToken ct = default)
    {
        var prompt = BuildCompletionPrompt(document, caretOffset);
        var response = await _chatService.SendMessageAsync(prompt, document.FilePath, ct).ConfigureAwait(false);
        return CleanCompletion(response ?? string.Empty);
    }

    public async Task<string> AskAsync(string question, CodeDocument? document, string? selectedText, string? workspaceSummary, CancellationToken ct = default)
    {
        var prompt = BuildSideChatPrompt(question, document, selectedText, workspaceSummary);
        var response = await _chatService.SendMessageAsync(prompt, document?.FilePath, ct).ConfigureAwait(false);
        return response ?? string.Empty;
    }

    public static string BuildCompletionPrompt(CodeDocument document, int caretOffset)
    {
        caretOffset = Math.Clamp(caretOffset, 0, document.Text.Length);
        var prefixStart = Math.Max(0, caretOffset - PrefixLimit);
        var suffixEnd = Math.Min(document.Text.Length, caretOffset + SuffixLimit);
        var prefix = document.Text[prefixStart..caretOffset];
        var suffix = document.Text[caretOffset..suffixEnd];

        return $$"""
You are Saturn UI's inline coding assistant. Complete the code at <cursor>.
Return only the code that should be inserted at the cursor. Do not use Markdown fences.
Language: {{document.Language}}
File: {{document.FilePath}}

<prefix>
{{prefix}}
</prefix>
<cursor />
<suffix>
{{suffix}}
</suffix>
""";
    }

    public static string BuildSideChatPrompt(string question, CodeDocument? document, string? selectedText, string? workspaceSummary)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are Saturn UI's coding assistant. Answer concisely and focus on the active code context.");

        if (!string.IsNullOrWhiteSpace(workspaceSummary))
        {
            builder.AppendLine();
            builder.AppendLine("Workspace tree summary:");
            builder.AppendLine(TrimTo(workspaceSummary, 6000));
        }

        if (document != null)
        {
            builder.AppendLine();
            builder.AppendLine("Active file:");
            builder.AppendLine($"Path: {document.FilePath}");
            builder.AppendLine($"Language: {document.Language}");
            builder.AppendLine("Content:");
            builder.AppendLine(TrimTo(document.Text, FileContextLimit));
        }

        if (!string.IsNullOrWhiteSpace(selectedText))
        {
            builder.AppendLine();
            builder.AppendLine("Selected text:");
            builder.AppendLine(TrimTo(selectedText, 6000));
        }

        builder.AppendLine();
        builder.AppendLine("User question:");
        builder.AppendLine(question);
        return builder.ToString();
    }

    public static string CleanCompletion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = value.Trim();
        if (text.StartsWith("[ERROR]", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var fenced = Regex.Match(text, "```(?:[a-zA-Z0-9_+-]+)?\\s*(?<code>[\\s\\S]*?)```", RegexOptions.Singleline);
        if (fenced.Success)
            text = fenced.Groups["code"].Value.Trim();

        if (text.StartsWith("Here is", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("Here's", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("Sure", StringComparison.OrdinalIgnoreCase))
        {
            var firstNewLine = text.IndexOf('\n');
            if (firstNewLine >= 0 && firstNewLine + 1 < text.Length)
                text = text[(firstNewLine + 1)..].Trim();
        }

        return text.Trim();
    }

    private static string TrimTo(string value, int maxChars)
    {
        if (value.Length <= maxChars)
            return value;

        return value[..maxChars] + Environment.NewLine + "...<truncated>";
    }
}

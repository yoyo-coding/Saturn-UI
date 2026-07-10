using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using SaturnUI.Models;

namespace SaturnUI.Services.Coding;

public sealed class CodeHighlightingService
{
    public void Apply(TextEditor editor, ProgrammingLanguage language)
    {
        var transformers = editor.TextArea.TextView.LineTransformers;
        for (var i = transformers.Count - 1; i >= 0; i--)
        {
            if (transformers[i] is CodeSyntaxColorizer)
                transformers.RemoveAt(i);
        }

        if (language != ProgrammingLanguage.PlainText)
            transformers.Add(new CodeSyntaxColorizer(language));

        editor.TextArea.TextView.Redraw();
    }
}

public sealed class CodeSyntaxColorizer : DocumentColorizingTransformer
{
    private static readonly Regex StringRegex = new("\"(?:\\\\.|[^\\\"])*\"|'(?:\\\\.|[^\\'])*'", RegexOptions.Compiled);
    private static readonly Regex NumberRegex = new("\\b(?:0x[0-9a-fA-F]+|\\d+(?:\\.\\d+)?)\\b", RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new("</?[A-Za-z][A-Za-z0-9:-]*|/?>", RegexOptions.Compiled);
    private static readonly Regex HtmlAttributeRegex = new("\\s[A-Za-z_:][A-Za-z0-9_.:-]*(?=\\=)", RegexOptions.Compiled);

    private static readonly IBrush KeywordBrush = new SolidColorBrush(Color.FromRgb(187, 134, 252));
    private static readonly IBrush TypeBrush = new SolidColorBrush(Color.FromRgb(128, 203, 196));
    private static readonly IBrush StringBrush = new SolidColorBrush(Color.FromRgb(195, 232, 141));
    private static readonly IBrush CommentBrush = new SolidColorBrush(Color.FromRgb(117, 117, 117));
    private static readonly IBrush NumberBrush = new SolidColorBrush(Color.FromRgb(247, 140, 108));
    private static readonly IBrush HtmlTagBrush = new SolidColorBrush(Color.FromRgb(255, 203, 107));
    private static readonly IBrush HtmlAttributeBrush = new SolidColorBrush(Color.FromRgb(130, 170, 255));

    private readonly ProgrammingLanguage _language;

    public CodeSyntaxColorizer(ProgrammingLanguage language)
    {
        _language = language;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.GetText(line);
        if (string.IsNullOrEmpty(text))
            return;

        if (_language == ProgrammingLanguage.Html)
        {
            ColorizeHtml(line, text);
            return;
        }

        ColorizeComment(line, text);
        ColorizeMatches(line, text, StringRegex, StringBrush);
        ColorizeMatches(line, text, NumberRegex, NumberBrush);
        ColorizeKeywords(line, text);
    }

    private void ColorizeHtml(DocumentLine line, string text)
    {
        var commentStart = text.IndexOf("<!--", StringComparison.Ordinal);
        if (commentStart >= 0)
        {
            var commentEnd = text.IndexOf("-->", commentStart + 4, StringComparison.Ordinal);
            var end = commentEnd >= 0 ? commentEnd + 3 : text.Length;
            ApplyBrush(line.Offset + commentStart, line.Offset + end, CommentBrush);
        }

        ColorizeMatches(line, text, HtmlTagRegex, HtmlTagBrush);
        ColorizeMatches(line, text, HtmlAttributeRegex, HtmlAttributeBrush);
        ColorizeMatches(line, text, StringRegex, StringBrush);
    }

    private void ColorizeComment(DocumentLine line, string text)
    {
        var marker = _language == ProgrammingLanguage.Python ? "#" : "//";
        var index = text.IndexOf(marker, StringComparison.Ordinal);
        if (index >= 0)
            ApplyBrush(line.Offset + index, line.EndOffset, CommentBrush);
    }

    private void ColorizeKeywords(DocumentLine line, string text)
    {
        foreach (var keyword in GetKeywords(_language))
        {
            foreach (Match match in Regex.Matches(text, $"\\b{Regex.Escape(keyword)}\\b"))
            {
                var brush = IsTypeKeyword(keyword) ? TypeBrush : KeywordBrush;
                ApplyBrush(line.Offset + match.Index, line.Offset + match.Index + match.Length, brush);
            }
        }
    }

    private void ColorizeMatches(DocumentLine line, string text, Regex regex, IBrush brush)
    {
        foreach (Match match in regex.Matches(text))
            ApplyBrush(line.Offset + match.Index, line.Offset + match.Index + match.Length, brush);
    }

    private void ApplyBrush(int startOffset, int endOffset, IBrush brush)
    {
        if (endOffset <= startOffset)
            return;

        ChangeLinePart(startOffset, endOffset, element => element.TextRunProperties.SetForegroundBrush(brush));
    }

    private static IReadOnlyCollection<string> GetKeywords(ProgrammingLanguage language) => language switch
    {
        ProgrammingLanguage.Python => new[] { "and", "as", "assert", "async", "await", "break", "class", "continue", "def", "del", "elif", "else", "except", "False", "finally", "for", "from", "global", "if", "import", "in", "is", "lambda", "None", "nonlocal", "not", "or", "pass", "raise", "return", "True", "try", "while", "with", "yield" },
        ProgrammingLanguage.C => new[] { "auto", "break", "case", "char", "const", "continue", "default", "do", "double", "else", "enum", "extern", "float", "for", "goto", "if", "inline", "int", "long", "register", "restrict", "return", "short", "signed", "sizeof", "static", "struct", "switch", "typedef", "union", "unsigned", "void", "volatile", "while" },
        ProgrammingLanguage.Cpp => new[] { "alignas", "auto", "bool", "break", "case", "catch", "char", "class", "concept", "const", "constexpr", "continue", "decltype", "default", "delete", "do", "double", "else", "enum", "explicit", "export", "extern", "false", "float", "for", "friend", "if", "inline", "int", "long", "namespace", "new", "noexcept", "nullptr", "operator", "private", "protected", "public", "return", "short", "signed", "sizeof", "static", "struct", "switch", "template", "this", "throw", "true", "try", "typedef", "typename", "union", "unsigned", "using", "virtual", "void", "volatile", "while" },
        ProgrammingLanguage.Java => new[] { "abstract", "assert", "boolean", "break", "byte", "case", "catch", "char", "class", "const", "continue", "default", "do", "double", "else", "enum", "extends", "final", "finally", "float", "for", "if", "implements", "import", "instanceof", "int", "interface", "long", "native", "new", "null", "package", "private", "protected", "public", "return", "short", "static", "strictfp", "super", "switch", "synchronized", "this", "throw", "throws", "transient", "true", "try", "void", "volatile", "while" },
        ProgrammingLanguage.Go => new[] { "break", "case", "chan", "const", "continue", "default", "defer", "else", "fallthrough", "for", "func", "go", "goto", "if", "import", "interface", "map", "package", "range", "return", "select", "struct", "switch", "type", "var", "bool", "byte", "complex64", "complex128", "error", "float32", "float64", "int", "int8", "int16", "int32", "int64", "rune", "string", "uint", "uint8", "uint16", "uint32", "uint64", "uintptr" },
        _ => Array.Empty<string>(),
    };

    private static bool IsTypeKeyword(string keyword) => keyword is "int" or "long" or "short" or "float" or "double" or "char" or "bool" or "boolean" or "byte" or "string" or "void" or "uint" or "rune";
}

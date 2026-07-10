using System.IO;
using SaturnUI.Models;

namespace SaturnUI.Services.Coding;

public sealed class CodeLanguageService
{
    public ProgrammingLanguage DetectLanguage(string? filePath)
    {
        var extension = Path.GetExtension(filePath ?? string.Empty).ToLowerInvariant();
        return extension switch
        {
            ".py" => ProgrammingLanguage.Python,
            ".c" or ".h" => ProgrammingLanguage.C,
            ".cpp" or ".cc" or ".cxx" or ".hpp" or ".hh" => ProgrammingLanguage.Cpp,
            ".java" => ProgrammingLanguage.Java,
            ".go" => ProgrammingLanguage.Go,
            ".html" or ".htm" => ProgrammingLanguage.Html,
            _ => ProgrammingLanguage.PlainText,
        };
    }

    public bool IsSupportedCodeFile(string? filePath) => DetectLanguage(filePath) != ProgrammingLanguage.PlainText;
}

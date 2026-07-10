using SaturnUI.Models;
using SaturnUI.Services.Coding;

namespace SaturnUI.Tests;

public class CodingServicesTests
{
    [Theory]
    [InlineData("main.py", ProgrammingLanguage.Python)]
    [InlineData("lib.c", ProgrammingLanguage.C)]
    [InlineData("lib.h", ProgrammingLanguage.C)]
    [InlineData("main.cpp", ProgrammingLanguage.Cpp)]
    [InlineData("main.cc", ProgrammingLanguage.Cpp)]
    [InlineData("main.cxx", ProgrammingLanguage.Cpp)]
    [InlineData("lib.hpp", ProgrammingLanguage.Cpp)]
    [InlineData("Main.java", ProgrammingLanguage.Java)]
    [InlineData("main.go", ProgrammingLanguage.Go)]
    [InlineData("index.html", ProgrammingLanguage.Html)]
    [InlineData("index.htm", ProgrammingLanguage.Html)]
    [InlineData("readme.md", ProgrammingLanguage.PlainText)]
    public void DetectLanguageMapsSupportedExtensions(string fileName, ProgrammingLanguage expected)
    {
        var service = new CodeLanguageService();
        Assert.Equal(expected, service.DetectLanguage(fileName));
    }

    [Fact]
    public void BuildWorkspaceTreeFiltersIgnoredDirectories()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(temp.Path, ".git"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "bin"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "obj"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "node_modules"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "src"));
        File.WriteAllText(Path.Combine(temp.Path, "src", "main.py"), "print('hi')");
        File.WriteAllText(Path.Combine(temp.Path, "readme.md"), "# docs");

        var service = new CodeFileService(new CodeLanguageService());
        var root = service.BuildWorkspaceTree(temp.Path);
        var names = root.Children.Select(c => c.Name).ToArray();

        Assert.DoesNotContain(".git", names);
        Assert.DoesNotContain("bin", names);
        Assert.DoesNotContain("obj", names);
        Assert.DoesNotContain("node_modules", names);
        Assert.Contains("src", names);
        Assert.Contains("readme.md", names);
    }

    [Theory]
    [InlineData("return x + 1;", "return x + 1;")]
    [InlineData("```python\nprint('hi')\n```", "print('hi')")]
    [InlineData("Here is the code:\nfor i in range(3):\n    print(i)", "for i in range(3):\n    print(i)")]
    public void CleanCompletionExtractsInsertableCode(string response, string expected)
    {
        Assert.Equal(expected, CodeAssistantService.CleanCompletion(response));
    }

    [Fact]
    public void BuildCompletionPromptIncludesLanguagePathAndLocalContext()
    {
        var text = string.Join('\n', Enumerable.Range(0, 800).Select(i => $"line {i}"));
        var document = new CodeDocument("C:\\src\\main.py", text, ProgrammingLanguage.Python);
        var prompt = CodeAssistantService.BuildCompletionPrompt(document, text.Length);

        Assert.Contains("Language: Python", prompt);
        Assert.Contains("File: C:\\src\\main.py", prompt);
        Assert.Contains("<prefix>", prompt);
        Assert.Contains("<suffix>", prompt);
        Assert.True(prompt.Length < 6000);
    }

    [Fact]
    public async Task SaveFileMarksDocumentClean()
    {
        using var temp = new TempDirectory();
        var file = Path.Combine(temp.Path, "main.py");
        File.WriteAllText(file, "print('old')");
        var service = new CodeFileService(new CodeLanguageService());
        var document = await service.OpenFileAsync(file);

        document.Text = "print('new')";
        Assert.True(document.IsDirty);

        await service.SaveFileAsync(document);

        Assert.False(document.IsDirty);
        Assert.Equal("print('new')", File.ReadAllText(file));
    }
}

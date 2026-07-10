using System;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SaturnUI.Models;

public partial class CodeDocument : ObservableObject
{
    private string _savedText;

    public CodeDocument(string filePath, string text, ProgrammingLanguage language)
    {
        FilePath = filePath;
        FileName = System.IO.Path.GetFileName(filePath);
        Language = language;
        _text = text;
        _savedText = text;
    }

    public string FilePath { get; }

    public string FileName { get; }

    public ProgrammingLanguage Language { get; }

    public string LanguageName => Language.ToString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private string _text = string.Empty;

    public bool IsDirty => !string.Equals(Text, _savedText, StringComparison.Ordinal);

    public string DisplayName => IsDirty ? $"{FileName} *" : FileName;

    public void MarkSaved(string text)
    {
        _savedText = text;
        Text = text;
    }
}

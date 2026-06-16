using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AspireLove.Studio.Highlighting;

/// <summary>
/// Lightweight C# syntax highlighter for the read-only AppHost.cs preview. Tokenizes the source
/// and renders coloured <see cref="Run"/>s into a <see cref="RichTextBox"/> via an attached
/// <c>Source</c> property, so the view stays bindable. Colours match the website's code blocks.
/// </summary>
public static class CodeHighlighter
{
    // Brand-matching dark-theme palette (same hues as the website's code snippets).
    private static readonly Brush DefaultBrush = Freeze(0xD7, 0xD5, 0xE6);
    private static readonly Brush CommentBrush = Freeze(0x6B, 0x69, 0x80);
    private static readonly Brush StringBrush = Freeze(0xC3, 0xE8, 0x8D);
    private static readonly Brush KeywordBrush = Freeze(0xC7, 0x92, 0xEA);
    private static readonly Brush MethodBrush = Freeze(0x82, 0xAA, 0xFF);
    private static readonly Brush NumberBrush = Freeze(0xF7, 0x8C, 0x6C);

    private static readonly Regex Tokenizer = new(
        """
        (?<comment>//[^\n]*|/\*.*?\*/)
        |(?<string>@"(?:""|[^"])*"|"(?:\\.|[^"\\\n])*")
        |(?<keyword>\b(?:using|namespace|class|struct|record|interface|enum|public|private|internal|protected|static|readonly|const|sealed|partial|override|virtual|abstract|async|await|var|new|return|if|else|for|foreach|while|do|switch|case|break|continue|throw|try|catch|finally|in|out|ref|this|base|null|true|false|void|string|int|bool|double|float|long|object|get|set)\b)
        |(?<method>[A-Za-z_]\w*(?=\s*\())
        |(?<number>\b\d+(?:\.\d+)?\b)
        """,
        RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.RegisterAttached(
            "Source", typeof(string), typeof(CodeHighlighter),
            new PropertyMetadata(string.Empty, OnSourceChanged));

    public static void SetSource(DependencyObject element, string value) => element.SetValue(SourceProperty, value);
    public static string GetSource(DependencyObject element) => (string)element.GetValue(SourceProperty);

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox box)
            return;

        var paragraph = new Paragraph { Margin = new Thickness(0), LineHeight = 17 };
        foreach (var run in Highlight(e.NewValue as string ?? string.Empty))
            paragraph.Inlines.Add(run);

        // A wide page width keeps long lines on one line; the RichTextBox scrolls horizontally.
        box.Document = new FlowDocument(paragraph) { PageWidth = 2400 };
    }

    private static IEnumerable<Run> Highlight(string code)
    {
        var index = 0;
        foreach (Match match in Tokenizer.Matches(code))
        {
            if (match.Index > index)
                yield return Token(code[index..match.Index], DefaultBrush);

            yield return Token(match.Value, BrushFor(match));
            index = match.Index + match.Length;
        }

        if (index < code.Length)
            yield return Token(code[index..], DefaultBrush);
    }

    private static Run Token(string text, Brush brush) => new(text) { Foreground = brush };

    private static Brush BrushFor(Match match) =>
        match.Groups["comment"].Success ? CommentBrush
        : match.Groups["string"].Success ? StringBrush
        : match.Groups["keyword"].Success ? KeywordBrush
        : match.Groups["method"].Success ? MethodBrush
        : match.Groups["number"].Success ? NumberBrush
        : DefaultBrush;

    private static SolidColorBrush Freeze(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}

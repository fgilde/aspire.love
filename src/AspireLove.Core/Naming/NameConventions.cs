using System.Globalization;
using System.Text;

namespace AspireLove.Core.Naming;

/// <summary>
/// Turns an arbitrary project name into the various shapes the generated artifacts need:
/// a PascalCase C# identifier (assembly/namespace) and a lowercase slug (Aspire resource names).
/// </summary>
public static class NameConventions
{
    /// <summary>
    /// "My Cool App" → "MyCoolApp". Guarantees a valid C# identifier start (prefixes "_" if needed).
    /// </summary>
    public static string ToPascalIdentifier(string name)
    {
        var parts = SplitWords(name);
        if (parts.Count == 0)
            return "MyAspireLove";

        var builder = new StringBuilder();
        foreach (var part in parts)
            builder.Append(char.ToUpper(part[0], CultureInfo.InvariantCulture)).Append(part[1..]);

        var result = builder.ToString();
        return char.IsLetter(result[0]) || result[0] == '_' ? result : "_" + result;
    }

    /// <summary>
    /// "My Cool App" → "my-cool-app". Suitable for Aspire resource names (lowercase, hyphenated).
    /// </summary>
    public static string ToResourceSlug(string name)
    {
        var parts = SplitWords(name);
        return parts.Count == 0
            ? "myaspirelove"
            : string.Join('-', parts.Select(p => p.ToLowerInvariant()));
    }

    private static List<string> SplitWords(string name)
    {
        var words = new List<string>();
        var current = new StringBuilder();

        foreach (var ch in name ?? string.Empty)
        {
            if (char.IsLetterOrDigit(ch))
            {
                current.Append(ch);
            }
            else if (current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
            words.Add(current.ToString());

        return words;
    }
}

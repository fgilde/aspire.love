using System.Text.Json;

namespace AspireLove.Core.Resolution;

/// <summary>
/// Reads the <c>name</c> field from a Lovable project's package.json.
/// </summary>
public static class PackageJsonReader
{
    /// <summary>
    /// Lovable scaffolds every new project with this generic name, so it is useless as a
    /// project name and we treat it as "absent".
    /// </summary>
    private static readonly HashSet<string> GenericNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "vite_react_shadcn_ts",
    };

    /// <summary>
    /// Returns the package name, or null when the file is missing, unreadable, empty or a
    /// known generic Lovable placeholder.
    /// </summary>
    public static string? TryReadProjectName(string lovableProjectPath)
    {
        var packageJsonPath = Path.Combine(lovableProjectPath, "package.json");
        if (!File.Exists(packageJsonPath))
            return null;

        try
        {
            using var stream = File.OpenRead(packageJsonPath);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("name", out var nameElement))
                return null;

            var name = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(name) || GenericNames.Contains(name))
                return null;

            return name;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool IsGenericName(string name) => GenericNames.Contains(name);
}

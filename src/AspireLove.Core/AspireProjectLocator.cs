namespace AspireLove.Core;

/// <summary>Where a previously generated Aspire AppHost lives inside a Lovable project.</summary>
public sealed record AspireProjectInfo(string AspireRoot, string AppHostDirectory, string AppHostProjectPath);

/// <summary>
/// Finds an already-generated <c>aspire</c> folder so the UI can offer "launch" and "publish"
/// actions. Matches on the <c>*.AppHost</c> convention rather than an exact name, so a renamed
/// project is still detected.
/// </summary>
public static class AspireProjectLocator
{
    public static AspireProjectInfo? Locate(string? lovableProjectPath)
    {
        if (string.IsNullOrWhiteSpace(lovableProjectPath))
            return null;

        var aspireRoot = Path.Combine(lovableProjectPath, "aspire");
        if (!Directory.Exists(aspireRoot))
            return null;

        foreach (var dir in Directory.EnumerateDirectories(aspireRoot, "*.AppHost"))
        {
            var csproj = Path.Combine(dir, Path.GetFileName(dir) + ".csproj");
            if (File.Exists(csproj))
                return new AspireProjectInfo(aspireRoot, dir, csproj);
        }

        return null;
    }
}

using AspireLove.Core.Options;

namespace AspireLove.Core.Resolution;

/// <summary>
/// Fills in the defaults that depend on the target Lovable project (primarily the project
/// name from package.json) so callers only have to provide what the user actually typed.
/// </summary>
public static class OptionsResolver
{
    public const string FallbackProjectName = "MyAspireLove";

    public static GenerationOptions Resolve(GenerationOptions options)
    {
        var name = ResolveProjectName(options);
        return options with { ProjectName = name };
    }

    private static string ResolveProjectName(GenerationOptions options)
    {
        // An explicit, non-empty user choice always wins.
        if (!string.IsNullOrWhiteSpace(options.ProjectName))
            return options.ProjectName.Trim();

        // Otherwise derive from package.json, falling back to a safe default.
        return PackageJsonReader.TryReadProjectName(options.LovableProjectPath)
               ?? FallbackProjectName;
    }
}

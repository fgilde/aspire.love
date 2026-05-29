using AspireLove.Core.Generation;
using AspireLove.Core.Options;
using AspireLove.Core.Resolution;
using AspireLove.Core.Validation;

namespace AspireLove.Core;

/// <summary>
/// The public entry point shared by the CLI and the WPF UI. Wraps option resolution,
/// validation and file generation into one cohesive surface.
/// </summary>
public sealed class AspireLoveGenerator
{
    private readonly ProjectGenerator _generator = new();

    /// <summary>Fills in defaults (notably the project name from package.json).</summary>
    public GenerationOptions Resolve(GenerationOptions options) => OptionsResolver.Resolve(options);

    /// <summary>Checks the options against the target project; never writes anything.</summary>
    public ValidationResult Validate(GenerationOptions options) => GenerationValidator.Validate(options);

    /// <summary>Renders the files in memory without touching disk (preview / --dry-run).</summary>
    public IReadOnlyList<GeneratedFile> Preview(GenerationOptions options) => _generator.Generate(options);

    /// <summary>
    /// Generates the project. Throws <see cref="GenerationValidationException"/> if validation
    /// fails. When <see cref="GenerationOptions.DryRun"/> is set, files are returned but not written.
    /// </summary>
    public GenerationOutcome Run(GenerationOptions options)
    {
        var validation = Validate(options);
        if (!validation.IsValid)
            throw new GenerationValidationException(validation);

        var files = _generator.Generate(options);
        var aspireRoot = Path.Combine(options.LovableProjectPath, "aspire");

        if (options.DryRun)
            return new GenerationOutcome(aspireRoot, files, FilesWritten: false, PackageJson: null);

        WriteFiles(aspireRoot, files);
        var packageJson = PackageJsonScriptUpdater.EnsureAspireScript(options.LovableProjectPath);

        return new GenerationOutcome(aspireRoot, files, FilesWritten: true, packageJson);
    }

    private static void WriteFiles(string aspireRoot, IReadOnlyList<GeneratedFile> files)
    {
        foreach (var file in files)
        {
            var fullPath = Path.Combine(aspireRoot, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, file.Content);
        }
    }
}

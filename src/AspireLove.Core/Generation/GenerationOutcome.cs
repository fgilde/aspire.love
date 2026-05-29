namespace AspireLove.Core.Generation;

/// <summary>
/// The result of a generation run: which files were produced (and whether they were written
/// to disk) plus what happened to the Lovable project's package.json.
/// </summary>
public sealed record GenerationOutcome(
    string OutputRoot,
    IReadOnlyList<GeneratedFile> Files,
    bool FilesWritten,
    PackageJsonUpdateOutcome? PackageJson);

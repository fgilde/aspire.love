namespace AspireLove.Core.Generation;

/// <summary>
/// A single file produced by the generator. <see cref="RelativePath"/> is relative to the
/// generated <c>aspire</c> folder and uses forward slashes.
/// </summary>
public sealed record GeneratedFile(string RelativePath, string Content);

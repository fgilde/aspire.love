namespace AspireLove.Core.Tests;

/// <summary>
/// A throwaway directory that mimics a Lovable project root. Disposing removes it, so each
/// test gets an isolated sandbox and never touches the read-only reference project.
/// </summary>
internal sealed class TempProject : IDisposable
{
    public TempProject()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "alove-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public TempProject WithPackageJson(string content)
    {
        File.WriteAllText(System.IO.Path.Combine(Path, "package.json"), content);
        return this;
    }

    public TempProject WithPackageName(string name) =>
        WithPackageJson($$"""{ "name": "{{name}}", "scripts": { "dev": "vite" } }""");

    public TempProject WithSupabaseLayout()
    {
        Directory.CreateDirectory(System.IO.Path.Combine(Path, "supabase", "migrations"));
        Directory.CreateDirectory(System.IO.Path.Combine(Path, "supabase", "functions"));
        return this;
    }

    public string ReadPackageJson() => File.ReadAllText(System.IO.Path.Combine(Path, "package.json"));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup — a locked file should not fail the test run.
        }
    }
}

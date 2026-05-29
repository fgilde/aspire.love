using AspireLove.Core;
using AspireLove.Core.Generation;
using AspireLove.Core.Options;

namespace AspireLove.Core.Tests;

public class AspireLoveGeneratorTests
{
    private readonly AspireLoveGenerator _sut = new();

    [Fact]
    public void Run_throws_when_validation_fails()
    {
        using var project = new TempProject().WithPackageName("app");

        var options = new GenerationOptions
        {
            LovableProjectPath = project.Path,
            Mode = GenerationMode.SupabaseSync, // missing SyncInfo → invalid
        };

        var ex = Assert.Throws<GenerationValidationException>(() => _sut.Run(options));
        Assert.False(ex.Result.IsValid);
    }

    [Fact]
    public void Dry_run_writes_nothing_to_disk()
    {
        using var project = new TempProject().WithPackageName("app");

        var outcome = _sut.Run(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            ProjectName = "App",
            DryRun = true,
        });

        Assert.False(outcome.FilesWritten);
        Assert.Null(outcome.PackageJson);
        Assert.False(Directory.Exists(Path.Combine(project.Path, "aspire")));
        Assert.NotEmpty(outcome.Files);
    }

    [Fact]
    public void Run_writes_files_and_updates_package_json()
    {
        using var project = new TempProject().WithPackageJson("""{ "name": "app", "scripts": { "dev": "vite" } }""");

        var outcome = _sut.Run(new GenerationOptions
        {
            LovableProjectPath = project.Path,
            ProjectName = "App",
        });

        Assert.True(outcome.FilesWritten);
        Assert.Equal(PackageJsonUpdateOutcome.Added, outcome.PackageJson);
        Assert.True(File.Exists(Path.Combine(project.Path, "aspire", "App.AppHost", "AppHost.cs")));
        Assert.True(File.Exists(Path.Combine(project.Path, "aspire", "App.slnx")));
        Assert.Contains("\"aspire\"", project.ReadPackageJson());
    }

    [Fact]
    public void Preview_returns_files_without_writing()
    {
        using var project = new TempProject().WithPackageName("app");

        var files = _sut.Preview(new GenerationOptions { LovableProjectPath = project.Path, ProjectName = "App" });

        Assert.NotEmpty(files);
        Assert.False(Directory.Exists(Path.Combine(project.Path, "aspire")));
    }
}

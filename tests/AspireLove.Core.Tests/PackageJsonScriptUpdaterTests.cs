using AspireLove.Core.Generation;

namespace AspireLove.Core.Tests;

public class PackageJsonScriptUpdaterTests
{
    [Fact]
    public void Returns_not_found_when_no_package_json()
    {
        using var project = new TempProject();

        Assert.Equal(PackageJsonUpdateOutcome.NotFound, PackageJsonScriptUpdater.EnsureAspireScript(project.Path));
    }

    [Fact]
    public void Returns_invalid_for_unparseable_json()
    {
        using var project = new TempProject().WithPackageJson("{ broken");

        Assert.Equal(PackageJsonUpdateOutcome.Invalid, PackageJsonScriptUpdater.EnsureAspireScript(project.Path));
    }

    [Fact]
    public void Adds_aspire_script_and_preserves_existing_scripts()
    {
        using var project = new TempProject().WithPackageJson(
            """{ "name": "app", "scripts": { "dev": "vite", "build": "vite build" } }""");

        var outcome = PackageJsonScriptUpdater.EnsureAspireScript(project.Path);

        Assert.Equal(PackageJsonUpdateOutcome.Added, outcome);
        var json = project.ReadPackageJson();
        Assert.Contains("\"aspire\": \"npm i && npm run dev\"", json);
        Assert.Contains("\"dev\": \"vite\"", json);
        Assert.Contains("\"build\": \"vite build\"", json);
    }

    [Fact]
    public void Does_not_escape_ampersands()
    {
        using var project = new TempProject().WithPackageJson("""{ "name": "app", "scripts": {} }""");

        PackageJsonScriptUpdater.EnsureAspireScript(project.Path);

        Assert.Contains("&&", project.ReadPackageJson());
        Assert.DoesNotContain("\\u0026", project.ReadPackageJson());
    }

    [Fact]
    public void Creates_scripts_object_when_absent()
    {
        using var project = new TempProject().WithPackageJson("""{ "name": "app" }""");

        var outcome = PackageJsonScriptUpdater.EnsureAspireScript(project.Path);

        Assert.Equal(PackageJsonUpdateOutcome.Added, outcome);
        Assert.Contains("\"aspire\"", project.ReadPackageJson());
    }

    [Fact]
    public void Is_idempotent_when_script_already_present()
    {
        using var project = new TempProject().WithPackageJson(
            """{ "name": "app", "scripts": { "aspire": "custom command" } }""");

        var outcome = PackageJsonScriptUpdater.EnsureAspireScript(project.Path);

        Assert.Equal(PackageJsonUpdateOutcome.AlreadyPresent, outcome);
        // Must not overwrite a user's existing script.
        Assert.Contains("custom command", project.ReadPackageJson());
    }
}

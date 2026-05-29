using AspireLove.Core.Options;
using AspireLove.Core.Resolution;

namespace AspireLove.Core.Tests;

public class OptionsResolverTests
{
    private static GenerationOptions OptionsFor(string path, string? projectName = null) =>
        new() { LovableProjectPath = path, ProjectName = projectName };

    [Fact]
    public void Explicit_name_wins_over_package_json()
    {
        using var project = new TempProject().WithPackageName("from-package");

        var resolved = OptionsResolver.Resolve(OptionsFor(project.Path, "Explicit Name"));

        Assert.Equal("Explicit Name", resolved.ProjectName);
    }

    [Fact]
    public void Explicit_name_is_trimmed()
    {
        using var project = new TempProject();

        var resolved = OptionsResolver.Resolve(OptionsFor(project.Path, "  Spaced  "));

        Assert.Equal("Spaced", resolved.ProjectName);
    }

    [Fact]
    public void Falls_back_to_package_json_name_when_no_explicit_name()
    {
        using var project = new TempProject().WithPackageName("my-lovable-app");

        var resolved = OptionsResolver.Resolve(OptionsFor(project.Path));

        Assert.Equal("my-lovable-app", resolved.ProjectName);
    }

    [Fact]
    public void Generic_lovable_placeholder_is_treated_as_absent()
    {
        using var project = new TempProject().WithPackageName("vite_react_shadcn_ts");

        var resolved = OptionsResolver.Resolve(OptionsFor(project.Path));

        Assert.Equal(OptionsResolver.FallbackProjectName, resolved.ProjectName);
    }

    [Fact]
    public void Falls_back_when_package_json_missing()
    {
        using var project = new TempProject();

        var resolved = OptionsResolver.Resolve(OptionsFor(project.Path));

        Assert.Equal(OptionsResolver.FallbackProjectName, resolved.ProjectName);
    }

    [Fact]
    public void Falls_back_when_package_json_is_invalid_json()
    {
        using var project = new TempProject().WithPackageJson("{ not valid json");

        var resolved = OptionsResolver.Resolve(OptionsFor(project.Path));

        Assert.Equal(OptionsResolver.FallbackProjectName, resolved.ProjectName);
    }
}

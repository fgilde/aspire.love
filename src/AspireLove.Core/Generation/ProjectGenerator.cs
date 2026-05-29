using System.Text.RegularExpressions;
using AspireLove.Core.Options;
using AspireLove.Core.Resolution;

namespace AspireLove.Core.Generation;

/// <summary>
/// Renders the full set of files for the generated <c>aspire</c> folder into memory. Writing
/// to disk and touching the Lovable project's package.json are handled separately so this
/// stays a pure, easily testable transformation.
/// </summary>
public sealed class ProjectGenerator
{
    private readonly TemplateRenderer _renderer;

    public ProjectGenerator() : this(new TemplateRenderer())
    {
    }

    public ProjectGenerator(TemplateRenderer renderer) => _renderer = renderer;

    public IReadOnlyList<GeneratedFile> Generate(GenerationOptions options)
    {
        var resolved = OptionsResolver.Resolve(options);
        var model = new TemplateModel(resolved);
        var appHost = model.AppHostProjectName;

        var files = new List<GeneratedFile>
        {
            new($"{model.SolutionName}.slnx", _renderer.Render("Solution.slnx", model)),
            new($"{appHost}/{appHost}.csproj", _renderer.Render("AppHost.csproj", model)),
            new($"{appHost}/AppHost.cs", Tidy(_renderer.Render("AppHostProgram", model))),
            new($"{appHost}/Constants.cs", Tidy(_renderer.Render("Constants", model))),
            new($"{appHost}/appsettings.json", _renderer.Render("appsettings.json", model)),
            new($"{appHost}/Properties/launchSettings.json", _renderer.Render("launchSettings.json", model)),
        };

        if (model.AddMonitoring)
        {
            // Empty marker so the dashboards folder the AppHost points at exists in source control.
            files.Add(new("observability/grafana/dashboards/.gitkeep", string.Empty));
        }

        return files;
    }

    /// <summary>Collapses runs of 3+ newlines into a single blank line so conditional template
    /// blocks don't leave double gaps in the generated C#.</summary>
    private static string Tidy(string content) =>
        Regex.Replace(content, @"(\r?\n){3,}", "$1$1");
}

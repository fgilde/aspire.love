using System.Reflection;
using Scriban;
using Scriban.Runtime;

namespace AspireLove.Core.Generation;

/// <summary>
/// Loads the embedded Scriban templates and renders them against a <see cref="TemplateModel"/>.
/// Member names are kept as-is (PascalCase) so templates read like the C# they produce.
/// </summary>
public sealed class TemplateRenderer
{
    private const string TemplatePrefix = "AspireLove.Core.Templates.";
    private static readonly Assembly ResourceAssembly = typeof(TemplateRenderer).Assembly;

    public string Render(string templateName, TemplateModel model)
    {
        var source = LoadTemplate(templateName);
        var template = Template.Parse(source, templateName);
        if (template.HasErrors)
        {
            var messages = string.Join(Environment.NewLine, template.Messages);
            throw new InvalidOperationException($"Template '{templateName}' has errors:{Environment.NewLine}{messages}");
        }

        return template.Render(model, member => member.Name);
    }

    private static string LoadTemplate(string templateName)
    {
        var resourceName = TemplatePrefix + templateName + ".scriban";
        using var stream = ResourceAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded template '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

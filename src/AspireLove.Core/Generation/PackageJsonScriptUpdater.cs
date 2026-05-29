using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AspireLove.Core.Generation;

public enum PackageJsonUpdateOutcome
{
    /// <summary>No package.json was found at the project root.</summary>
    NotFound,

    /// <summary>The file could not be parsed as JSON.</summary>
    Invalid,

    /// <summary>An "aspire" script already existed; nothing was changed.</summary>
    AlreadyPresent,

    /// <summary>The "aspire" script was added.</summary>
    Added,
}

/// <summary>
/// Ensures the Lovable project's package.json has the <c>aspire</c> npm script that Aspire's
/// <c>AddJavaScriptApp(..., "aspire")</c> launches. This is the only write outside the
/// generated <c>aspire</c> folder.
/// </summary>
public static class PackageJsonScriptUpdater
{
    public const string ScriptName = "aspire";
    public const string ScriptCommand = "npm i && npm run dev";

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        // Keep "&&", "<", ">" literal instead of \u00XX escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static PackageJsonUpdateOutcome EnsureAspireScript(string lovableProjectPath)
    {
        var path = Path.Combine(lovableProjectPath, "package.json");
        if (!File.Exists(path))
            return PackageJsonUpdateOutcome.NotFound;

        JsonObject root;
        try
        {
            if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject parsed)
                return PackageJsonUpdateOutcome.Invalid;
            root = parsed;
        }
        catch (JsonException)
        {
            return PackageJsonUpdateOutcome.Invalid;
        }

        if (root["scripts"] is not JsonObject scripts)
        {
            scripts = new JsonObject();
            root["scripts"] = scripts;
        }

        if (scripts.ContainsKey(ScriptName))
            return PackageJsonUpdateOutcome.AlreadyPresent;

        scripts[ScriptName] = ScriptCommand;
        File.WriteAllText(path, root.ToJsonString(WriteOptions) + Environment.NewLine);
        return PackageJsonUpdateOutcome.Added;
    }
}

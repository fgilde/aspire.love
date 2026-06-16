using System.Text.RegularExpressions;
using AspireLove.Core.Options;

namespace AspireLove.Core;

/// <summary>
/// Reconstructs <see cref="GenerationOptions"/> from an already-generated <c>aspire</c> folder by
/// parsing the values aspire.love itself wrote into AppHost.cs and Constants.cs. Lets the UI
/// pre-fill every field from an existing project so the user can just regenerate. Best-effort:
/// anything that can't be read falls back to the normal default.
/// </summary>
public static class ExistingProjectReader
{
    public static bool TryRead(string? lovableProjectPath, out GenerationOptions options)
    {
        options = null!;

        if (AspireProjectLocator.Locate(lovableProjectPath) is not { } project)
            return false;

        var appHostPath = Path.Combine(project.AppHostDirectory, "AppHost.cs");
        var constantsPath = Path.Combine(project.AppHostDirectory, "Constants.cs");
        if (!File.Exists(appHostPath) || !File.Exists(constantsPath))
            return false;

        // Drop comment lines: the generated code carries "to switch modes, use .WithProjectSync(…)"
        // style hints that name the *other* mode's methods and would fool structural detection.
        var appHost = StripComments(File.ReadAllText(appHostPath));
        var constants = File.ReadAllText(constantsPath);

        var mode = DetectMode(appHost);

        // User tuple: ("name", "email", "password").
        var user = Match(constants, """Default\s*=\s*\(\s*"([^"]*)"\s*,\s*"([^"]*)"\s*,\s*"([^"]*)"\s*\)""", 3);
        var defaultUser = user is { Length: 3 }
            ? new DefaultUser(user[0], user[1], user[2])
            : DefaultUser.CreateDefault();

        // WithProjectName only exists in the local-stack ConfigureStudio block; for Remote Connect
        // fall back to the AppHost folder name (e.g. "MyShop.AppHost" -> "MyShop").
        var projectName = Single(appHost, """WithProjectName\("([^"]*)"\)""")
            ?? StripAppHostSuffix(Path.GetFileName(project.AppHostDirectory));

        options = new GenerationOptions
        {
            LovableProjectPath = lovableProjectPath!,
            ProjectName = projectName,
            OrganizationName = Single(appHost, """WithOrganizationName\("([^"]*)"\)""") ?? "My Company",
            User = defaultUser,
            DatabasePassword = Single(appHost, """WithPassword\("([^"]*)"\)""") ?? "local-dev-password-123",
            LovableApiKey = Single(constants, """LovableApiKey\s*=\s*"([^"]*)"""),
            Mode = mode,
            AddMonitoring = appHost.Contains("AddObservabilityStack"),
            AddPersistentStorage = appHost.Contains("AddSupabaseNfsStorage"),
            AddDeployScript = File.Exists(Path.Combine(project.AspireRoot, "scripts", "deploy.ps1")),
            SyncInfo = mode == GenerationMode.SupabaseSync ? ReadSyncInfo(constants) : null,
            RemoteInfo = mode == GenerationMode.RemoteConnect ? ReadRemoteInfo(constants) : null,
        };

        return true;
    }

    private static string StripComments(string code) =>
        string.Join('\n', code.Split('\n').Where(line => !line.TrimStart().StartsWith("//")));

    private static string StripAppHostSuffix(string folderName) =>
        folderName.EndsWith(".AppHost", StringComparison.OrdinalIgnoreCase)
            ? folderName[..^".AppHost".Length]
            : folderName;

    private static GenerationMode DetectMode(string appHost)
    {
        if (!appHost.Contains("builder.AddSupabase("))
            return GenerationMode.RemoteConnect;
        return appHost.Contains(".WithProjectSync(") ? GenerationMode.SupabaseSync : GenerationMode.FullLocal;
    }

    private static SupabaseSyncInfo ReadSyncInfo(string constants) => new(
        Const(constants, "ProjectRef") ?? "",
        Const(constants, "ServiceKey") ?? "",
        Const(constants, "DbPassword") ?? "",
        Const(constants, "ManagementToken") ?? "");

    private static RemoteConnectInfo ReadRemoteInfo(string constants) => new(
        Const(constants, "ProjectRef") ?? "",
        Const(constants, "ServiceKey") ?? "");

    private static string? Const(string text, string name) =>
        Single(text, "public const string " + Regex.Escape(name) + "\\s*=\\s*\"([^\"]*)\"");

    private static string? Single(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.Singleline);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string[]? Match(string text, string pattern, int groups)
    {
        var m = Regex.Match(text, pattern, RegexOptions.Singleline);
        if (!m.Success)
            return null;

        var result = new string[groups];
        for (var i = 0; i < groups; i++)
            result[i] = m.Groups[i + 1].Value;
        return result;
    }
}

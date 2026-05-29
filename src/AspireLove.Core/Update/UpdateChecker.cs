using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace AspireLove.Core.Update;

/// <summary>Outcome of a version check against the latest GitHub release.</summary>
public sealed record UpdateCheckResult(bool UpdateAvailable, Version Current, Version Latest, string? ReleaseUrl);

/// <summary>
/// Checks GitHub releases for a newer version of the tool. Used by both the CLI (to offer
/// <c>dotnet tool update</c>) and potentially the desktop app. All failures are swallowed and
/// surfaced as "no update" so a missing network connection never breaks the tool.
/// </summary>
public sealed class UpdateChecker
{
    public const string Repository = "fgilde/aspire.love";
    public const string ToolPackageId = "aspire.love";

    private static readonly Uri LatestReleaseApi =
        new($"https://api.github.com/repos/{Repository}/releases/latest");

    private readonly HttpClient _http;

    public UpdateChecker(HttpClient? http = null) => _http = http ?? CreateDefaultClient();

    /// <summary>The version of the currently running tool, derived from its assembly.</summary>
    public static Version CurrentVersion
    {
        get
        {
            var info = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            // Informational version may carry a "+commit" suffix — strip it before parsing.
            if (info is { Length: > 0 } && Version.TryParse(info.Split('+', '-')[0], out var parsed))
                return parsed;

            return Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
        }
    }

    public async Task<UpdateCheckResult?> CheckAsync(Version current, CancellationToken cancellationToken = default)
    {
        try
        {
            var release = await _http.GetFromJsonAsync<GitHubRelease>(LatestReleaseApi, cancellationToken);
            if (release?.TagName is not { Length: > 0 } tag || !TryParseTag(tag, out var latest))
                return null;

            return new UpdateCheckResult(latest > current, current, latest, release.HtmlUrl);
        }
        catch
        {
            // Offline, rate-limited, no releases yet, malformed payload — treat as "no info".
            return null;
        }
    }

    /// <summary>"v1.2.3" or "1.2.3" → <see cref="Version"/>.</summary>
    public static bool TryParseTag(string tag, out Version version) =>
        Version.TryParse(tag.TrimStart('v', 'V'), out version!);

    private static HttpClient CreateDefaultClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        // GitHub's API rejects requests without a User-Agent.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("aspire.love-update-checker");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string? TagName,
        [property: JsonPropertyName("html_url")] string? HtmlUrl,
        [property: JsonPropertyName("prerelease")] bool Prerelease);
}

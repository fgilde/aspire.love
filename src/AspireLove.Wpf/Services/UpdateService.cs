using System.IO;
using Velopack;
using Velopack.Sources;

namespace AspireLove.Studio.Services;

/// <summary>
/// Wraps Velopack's update flow against the GitHub releases of the tool. Only works when the app
/// was installed via the Velopack Setup (see <see cref="IsManaged"/>); in a dev/portable build it
/// is a no-op so the rest of the UI keeps working.
/// </summary>
public sealed class UpdateService
{
    public const string Repository = "https://github.com/fgilde/aspire.love";

    private readonly UpdateManager? _mgr;

    public UpdateService()
    {
        try
        {
            _mgr = new UpdateManager(new GithubSource(Repository, null, prerelease: false));
        }
        catch (Exception ex)
        {
            Log("ctor", ex);
        }
    }

    /// <summary>True when the app runs from a Velopack install (only then can it self-update).</summary>
    public bool IsManaged => _mgr?.IsInstalled == true;

    public async Task<UpdateInfo?> CheckAsync()
    {
        if (_mgr is null || !IsManaged) return null;
        try
        {
            return await _mgr.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            Log("CheckAsync", ex);
            return null;
        }
    }

    public async Task<bool> DownloadAsync(UpdateInfo info, IProgress<int>? progress = null)
    {
        if (_mgr is null) return false;
        try
        {
            await _mgr.DownloadUpdatesAsync(info, progress is null ? null : p => progress.Report(p));
            return true;
        }
        catch (Exception ex)
        {
            Log("DownloadAsync", ex);
            return false;
        }
    }

    public void ApplyAndRestart(UpdateInfo info)
    {
        if (_mgr is null) return;
        try
        {
            _mgr.ApplyUpdatesAndRestart(info);
        }
        catch (Exception ex)
        {
            Log("ApplyAndRestart", ex);
        }
    }

    private static void Log(string where, Exception ex)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(AppContext.BaseDirectory, "aspire.love-studio.log"),
                $"[{DateTimeOffset.UtcNow:O}] [UpdateService.{where}] {ex}\n\n");
        }
        catch { }
    }
}

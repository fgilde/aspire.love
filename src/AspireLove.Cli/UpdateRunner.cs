using System.Diagnostics;
using AspireLove.Core.Update;

namespace AspireLove.Cli;

/// <summary>CLI-side glue around <see cref="UpdateChecker"/>: prints results and shells out to
/// <c>dotnet tool update</c> when the user runs the <c>update</c> command.</summary>
internal static class UpdateRunner
{
    public static async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var current = UpdateChecker.CurrentVersion;
        ConsoleReporter.WriteLine(ConsoleColor.Cyan, $"Current version: {current}");

        var result = await new UpdateChecker().CheckAsync(current, cancellationToken);
        if (result is null)
        {
            ConsoleReporter.WriteLine(ConsoleColor.Yellow,
                "Could not check for updates (offline or no releases published yet).");
            return 0;
        }

        if (!result.UpdateAvailable)
        {
            ConsoleReporter.WriteLine(ConsoleColor.Green, "You're on the latest version.");
            return 0;
        }

        ConsoleReporter.WriteLine(ConsoleColor.Magenta,
            $"A new version is available: {result.Latest} (you have {result.Current}).");
        if (result.ReleaseUrl is { Length: > 0 } url)
            Console.WriteLine($"Release notes: {url}");

        Console.WriteLine($"Updating via: dotnet tool update -g {UpdateChecker.ToolPackageId}");
        return RunDotnetToolUpdate();
    }

    /// <summary>Best-effort, non-blocking hint printed after other commands. Never throws.</summary>
    public static void NotifyIfUpdateAvailable()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var result = new UpdateChecker().CheckAsync(UpdateChecker.CurrentVersion, cts.Token)
                .GetAwaiter().GetResult();

            if (result is { UpdateAvailable: true })
            {
                ConsoleReporter.WriteLine(ConsoleColor.Magenta,
                    $"Update available: {result.Latest} (you have {result.Current}). " +
                    $"Run 'aspire-love update' to upgrade.");
            }
        }
        catch
        {
            // A failed background check must never affect the primary command.
        }
    }

    private static int RunDotnetToolUpdate()
    {
        try
        {
            var psi = new ProcessStartInfo("dotnet", $"tool update -g {UpdateChecker.ToolPackageId}")
            {
                UseShellExecute = false,
            };
            using var process = Process.Start(psi);
            if (process is null)
            {
                ConsoleReporter.WriteLine(ConsoleColor.Red, "Could not start 'dotnet'. Is the .NET SDK installed?");
                return 1;
            }

            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            ConsoleReporter.WriteLine(ConsoleColor.Red, $"Update failed: {ex.Message}");
            return 1;
        }
    }
}

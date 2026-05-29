using System.Diagnostics;
using System.Windows;
using AspireLove.Core.Update;
using AspireLove.Studio.Services;
using Wpf.Ui.Controls;

namespace AspireLove.Studio;

public partial class AboutWindow : FluentWindow
{
    private const string GitHubUrl = "https://github.com/fgilde/aspire.love";
    private const string WebsiteUrl = "https://www.aspire.love";

    private readonly UpdateService _updates = new();

    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Version {UpdateChecker.CurrentVersion.ToString(3)}";
    }

    private async void OnCheckForUpdates(object sender, RoutedEventArgs e)
    {
        UpdateButton.IsEnabled = false;
        UpdateStatus.Visibility = Visibility.Visible;
        UpdateStatus.Text = "Checking for updates…";

        try
        {
            if (_updates.IsManaged)
                await RunManagedUpdateAsync();
            else
                await ReportUnmanagedUpdateAsync();
        }
        finally
        {
            UpdateButton.IsEnabled = true;
        }
    }

    /// <summary>Installed via Setup → Velopack downloads and applies the update in place.</summary>
    private async Task RunManagedUpdateAsync()
    {
        var current = UpdateChecker.CurrentVersion.ToString(3);
        var info = await _updates.CheckAsync();
        if (info is null)
        {
            UpdateStatus.Text = $"You're on the latest version (v{current}).";
            return;
        }

        var newVer = info.TargetFullRelease.Version.ToString();
        var ask = System.Windows.MessageBox.Show(
            $"A newer version is available: v{newVer} (you have v{current}).\n\nDownload and restart now?",
            "aspire.love Studio update", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information);
        if (ask != System.Windows.MessageBoxResult.Yes)
        {
            UpdateStatus.Text = $"Update v{newVer} is ready — installation skipped.";
            return;
        }

        UpdateStatus.Text = "Downloading update…";
        var ok = await _updates.DownloadAsync(info, new Progress<int>(p => UpdateStatus.Text = $"Downloading update… {p}%"));
        if (!ok)
        {
            UpdateStatus.Text = "Download failed — see aspire.love-studio.log.";
            return;
        }

        UpdateStatus.Text = "Update installed — restarting…";
        _updates.ApplyAndRestart(info);
    }

    /// <summary>Dev/portable build → can't self-update; check GitHub releases and point to the installer.</summary>
    private async Task ReportUnmanagedUpdateAsync()
    {
        var current = UpdateChecker.CurrentVersion;
        var result = await new UpdateChecker().CheckAsync(current);

        if (result is null)
            UpdateStatus.Text = "Couldn't reach GitHub. Check your connection and try again.";
        else if (result.UpdateAvailable)
            UpdateStatus.Text = $"A newer version is available: v{result.Latest.ToString(3)} " +
                                $"(you have v{current.ToString(3)}). Open the website to download the installer.";
        else
            UpdateStatus.Text = $"You're on the latest version (v{current.ToString(3)}).";
    }

    private void OnOpenGitHub(object sender, RoutedEventArgs e) => OpenUrl(GitHubUrl);

    private void OnOpenWebsite(object sender, RoutedEventArgs e) => OpenUrl(WebsiteUrl);

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}

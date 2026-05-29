using System.Diagnostics;
using System.Windows;
using AspireLove.Core.Update;
using Wpf.Ui.Controls;

namespace AspireLove.Studio;

public partial class AboutWindow : FluentWindow
{
    private const string GitHubUrl = "https://github.com/fgilde/aspire.love";
    private const string WebsiteUrl = "https://www.aspire.love";

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

        var current = UpdateChecker.CurrentVersion;
        var result = await new UpdateChecker().CheckAsync(current);

        if (result is null)
            UpdateStatus.Text = "Couldn't reach GitHub. Check your connection and try again.";
        else if (result.UpdateAvailable)
            UpdateStatus.Text = $"A newer version is available: v{result.Latest.ToString(3)} "
                + $"(you have v{current.ToString(3)}). Open releases to download it.";
        else
            UpdateStatus.Text = $"You're on the latest version (v{current.ToString(3)}).";

        UpdateButton.IsEnabled = true;
    }

    private void OnOpenGitHub(object sender, RoutedEventArgs e) => OpenUrl(GitHubUrl);

    private void OnOpenWebsite(object sender, RoutedEventArgs e) => OpenUrl(WebsiteUrl);

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}

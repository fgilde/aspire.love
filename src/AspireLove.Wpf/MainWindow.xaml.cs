using System.Windows;
using AspireLove.Core.Update;
using AspireLove.Studio.Services;
using Wpf.Ui.Controls;

namespace AspireLove.Studio;

public partial class MainWindow : FluentWindow
{
    private readonly UpdateService _updates = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Silent startup check: only a managed (Setup-installed) build can self-update.
        if (!_updates.IsManaged) return;

        var info = await _updates.CheckAsync();
        if (info is null) return;

        var current = UpdateChecker.CurrentVersion.ToString(3);
        var newVer = info.TargetFullRelease.Version.ToString();
        var ask = System.Windows.MessageBox.Show(
            $"A newer version of aspire.love Studio is available: v{newVer} (you have v{current}).\n\nDownload and restart now?",
            "Update available", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information);
        if (ask != System.Windows.MessageBoxResult.Yes) return;

        if (await _updates.DownloadAsync(info))
            _updates.ApplyAndRestart(info);
    }

    private void OnShowAbout(object sender, RoutedEventArgs e) =>
        new AboutWindow { Owner = this }.ShowDialog();
}

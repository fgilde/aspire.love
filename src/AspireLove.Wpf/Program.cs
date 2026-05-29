using System.IO;
using Velopack;

namespace AspireLove.Studio;

/// <summary>
/// Explicit entry point so Velopack can run first. Its install/uninstall/update hooks are
/// triggered via command-line args on launch; if WPF starts before <see cref="VelopackApp"/>
/// those hooks never fire. Registered via <c>&lt;StartupObject&gt;</c> in the csproj.
/// </summary>
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // Velopack hooks ZUERST: exits the process itself on --veloapp-install / --uninstall / etc.
        try
        {
            VelopackApp.Build().Run();
        }
        catch (Exception ex)
        {
            // A Velopack failure must never block the normal app launch.
            try
            {
                File.AppendAllText(
                    Path.Combine(AppContext.BaseDirectory, "aspire.love-studio.log"),
                    $"[{DateTimeOffset.UtcNow:O}] [Velopack] {ex}\n\n");
            }
            catch { }
        }

        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }
}

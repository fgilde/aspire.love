using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace AspireLove.Studio;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Match the website accent (#a855f7) so buttons, toggles and focus rings stay on-brand.
        ApplicationAccentColorManager.Apply(Color.FromRgb(0xA8, 0x55, 0xF7));
    }
}

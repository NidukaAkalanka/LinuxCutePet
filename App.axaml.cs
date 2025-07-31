using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace PetViewerLinux;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Start with pre-cache window instead of main window
            // Main window will be created after caching is complete
            var startupWindow = new StartupManager();
            desktop.MainWindow = startupWindow.StartApplication();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
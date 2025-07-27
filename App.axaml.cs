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
            // Check if calibration is needed
            if (ConfigManager.IsCalibrationNeeded())
            {
                // Show calibration window first
                var calibrationWindow = new CalibrationWindow();
                calibrationWindow.Closed += (s, e) =>
                {
                    // After calibration is complete, show main window
                    desktop.MainWindow = new MainWindow();
                    desktop.MainWindow.Show();
                };
                desktop.MainWindow = calibrationWindow;
            }
            else
            {
                desktop.MainWindow = new MainWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
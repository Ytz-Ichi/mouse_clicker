using System.Windows;

using WpfApplication = System.Windows.Application;

namespace AutoClicker;

public partial class App : WpfApplication
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (MainWindow is MainWindow mw)
        {
            mw.ViewModel.Dispose();
        }
        base.OnExit(e);
    }
}

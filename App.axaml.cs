using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Atomex.Client.Desktop.Dialogs.Views;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels;
using Atomex.Client.Desktop.Views;

namespace Atomex.Client.Desktop
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow();
                var mainWindowViewModel = BuildMainWindowDataContext(mainWindow);

                if (mainWindowViewModel != null)
                {
                    mainWindow.DataContext = mainWindowViewModel;
                    desktop.MainWindow = mainWindow;
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private MainWindowViewModel? BuildMainWindowDataContext(MainWindow mainWindow)
        {
            return new MainWindowViewModel(
                new DialogService<DialogServiceView>(mainWindow));
        }
    }
}
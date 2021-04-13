using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Atomex.Client.Desktop.Dialogs.Views;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels;
using Atomex.Client.Desktop.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using Atomex.Client.Desktop.Common;
using Serilog;
using Atomex.Common.Configuration;
using Atomex.Core;
using Atomex.MarketData.Bitfinex;
using Atomex.Subsystems;
using Avalonia.Input.Platform;
using Avalonia.Styling;

namespace Atomex.Client.Desktop
{
    public class App : Application
    {
        public static IDialogService<ViewModelBase> DialogService;
        public static TemplateService TemplateService;
        public static IClipboard Clipboard;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            TemplateService = new TemplateService();
            Clipboard = AvaloniaLocator.Current.GetService<IClipboard>();

            // init logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            Log.Information("Application startup");

            var currenciesProvider = new CurrenciesProvider(CurrenciesConfiguration);
            var symbolsProvider = new SymbolsProvider(SymbolsConfiguration);

            // init Atomex client app
            AtomexApp = new AtomexApp()
                .UseCurrenciesProvider(currenciesProvider)
                .UseSymbolsProvider(symbolsProvider)
                .UseCurrenciesUpdater(new CurrenciesUpdater(currenciesProvider))
                .UseSymbolsUpdater(new SymbolsUpdater(symbolsProvider))
                .UseQuotesProvider(new BitfinexQuotesProvider(
                    currencies: currenciesProvider.GetCurrencies(Network.MainNet),
                    baseCurrency: BitfinexQuotesProvider.Usd));

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow();
                DialogService = new DialogService<DialogServiceView>(mainWindow);
                var mainWindowViewModel = new MainWindowViewModel(AtomexApp, mainWindow);

                mainWindow.DataContext = mainWindowViewModel;
                desktop.MainWindow = mainWindow;

                desktop.Exit += OnExit;
            }

            AtomexApp.Start();

            base.OnFrameworkInitializationCompleted();
        }


        void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
            {
                if (e.ApplicationExitCode == 300)
                {
                    SingleApp.CloseAndSwitch();
                    return;
                }

                AtomexApp.Stop();

                // try { Updater.Stop(); }
                // catch (TimeoutException) { Log.Error("Failed to stop the updater due to timeout"); }

                // update has been requested
                // if (e.ApplicationExitCode == 101)
                // {
                //     try
                //     {
                //         Updater.RunUpdate();
                //         Log.Information("Update scheduled");
                //     }
                //     catch (Exception ex)
                //     {
                //         Log.Error(ex, "Failed to schedule update");
                //     }
                // }

                Log.Information("Application shutdown");

                SingleApp.Close();
            }
        }

        public static IAtomexApp AtomexApp { get; private set; }

        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
#if DEBUG
            .AddJsonFile("config.debug.json")
#else
            .AddJsonFile("config.json")
#endif
            .Build();

        private static Assembly CoreAssembly { get; } = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Atomex.Client.Core");

        private static IConfiguration CurrenciesConfiguration { get; } = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEmbeddedJsonFile(CoreAssembly, "currencies.json")
            .Build();

        private static IConfiguration SymbolsConfiguration { get; } = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEmbeddedJsonFile(CoreAssembly, "symbols.json")
            .Build();

        public static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // If no associated application/json MimeType is found xdg-open opens retrun error
                // but it tries to open it anyway using the console editor (nano, vim, other..)
                ShellExec($"xdg-open {url}", waitForExit: false);
            }
            else
            {
                using (Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? url : "open",
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"{url}" : "",
                    CreateNoWindow = true,
                    UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                })) ;
            }
        }

        private static void ShellExec(string cmd, bool waitForExit = true)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            using (var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            ))
            {
                if (waitForExit)
                {
                    process.WaitForExit();
                }
            }
        }
    }
}
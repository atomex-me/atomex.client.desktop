using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Atomex.Client.Desktop.Dialogs.Views;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels;
using Atomex.Client.Desktop.Views;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Atomex.Client.Desktop.Common;
using Serilog;
using Atomex.Common.Configuration;
using Atomex.Core;
using Atomex.MarketData.Bitfinex;
using Atomex.Subsystems;
using Avalonia.Input.Platform;
using Avalonia.Styling;
using Nethereum.Contracts;
using Sentry;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Atomex.Client.Desktop
{
    public class App : Application
    {
        public static IDialogService<ViewModelBase> DialogService;
        public static TemplateService TemplateService;
        public static ImageService ImageService;
        public static IClipboard Clipboard;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            TemplateService = new TemplateService();
            ImageService = new ImageService();
            Clipboard = AvaloniaLocator.Current.GetService<IClipboard>();

            // init logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                // .WriteTo.Sentry(o =>
                // {
                //     o.Dsn = new Dsn("https://793b3e5e430143be9f9c240d83b9ff3f@sentry.baking-bad.org/8");
                //     // Debug and higher are stored as breadcrumbs (default is Information)
                //     o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
                //     // Warning and higher is sent as event (default is Error)
                //     o.MinimumEventLevel = LogEventLevel.Debug;
                // })
                .CreateLogger();

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

                // var sink = new InMemorySink(mainWindowViewModel.LogEvent);
                //
                // Log.Logger = new LoggerConfiguration()
                //     .WriteTo.Sink(sink)
                //     .CreateLogger();
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
                    FileName = "/bin/bash",
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

    class InMemorySink : ILogEventSink
    {
        private Action<string> _logAction;

        public InMemorySink(Action<string> logAction)
        {
            _logAction = logAction;
        }

        readonly ITextFormatter _textFormatter = new MessageTemplateTextFormatter("[{Level}] {Message}{Exception}");

        public ConcurrentQueue<string> Events { get; } = new ConcurrentQueue<string>();

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            var renderSpace = new StringWriter();
            _textFormatter.Format(logEvent, renderSpace);

            Events.Enqueue(renderSpace.ToString());

            _logAction.Invoke(renderSpace.ToString());
        }
    }
}
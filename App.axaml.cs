using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Serilog;
using Serilog.Events;

using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels;
using Atomex.Client.Desktop.Views;
using Atomex.Common.Configuration;
using Atomex.Core;
using Atomex.MarketData;
using Atomex.MarketData.Abstract;
using Atomex.MarketData.Bitfinex;
using Atomex.MarketData.TezTools;
using Atomex.Services;

namespace Atomex.Client.Desktop
{
    public class App : Application
    {
        public static DialogService DialogService;
        public static TemplateService TemplateService;
        public static IClipboard? Clipboard;
        public static NotificationsService NotificationsService;
        public static ILoggerFactory LoggerFactory;
        public static MainWindowViewModel MainWindowViewModel;
        public static Action<string> ConnectTezosDapp;

        private SingleInstanceLoopbackService _singleInstanceLoopbackService;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                Environment.Exit(0);
                return;
            }

            // set invariant culture by default
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            // configure loggers
            ConfigureLoggers();
            // MacUpdater.CheckForMacOsDeepLinks();

            UrlsOpened += (sender, args) =>
            {
                if (args.Urls.Length == 0) return;

                MainWindowViewModel.StartupData = args.Urls[0];
                Log.Information("Setting startup data from URLOpened {Url}", args.Urls[0]);
            };

            if (SingleInstanceLoopbackService.TrySendArgsToOtherInstance(desktop.Args, LoggerFactory.CreateLogger<SingleInstanceLoopbackService>()))
            {
                // arguments passed successfully to another application instance, exit
                Environment.Exit(0);
                return;
            }

            TemplateService = new TemplateService();
            Clipboard = AvaloniaLocator.Current.GetService<IClipboard>();

            var currenciesProvider = new CurrenciesProvider(CurrenciesConfigurationString);
            var symbolsProvider = new SymbolsProvider(SymbolsConfiguration);

            var bitfinexQuotesProvider = new BitfinexQuotesProvider(
                currencies: currenciesProvider
                    .GetCurrencies(Network.MainNet)
                    .GetOrderedPreset()
                    .Select(c => c.Name),
                baseCurrency: QuotesProvider.Usd,
                log: LoggerFactory.CreateLogger<BitfinexQuotesProvider>());

            var tezToolsQuotesProvider = new TezToolsQuotesProvider(
                log: LoggerFactory.CreateLogger<TezToolsQuotesProvider>());

            var quotesProvider = new MultiSourceQuotesProvider(
                log: LoggerFactory.CreateLogger<MultiSourceQuotesProvider>(),
                bitfinexQuotesProvider,
                tezToolsQuotesProvider);

            // init Atomex client app
            AtomexApp = new AtomexApp(logger: LoggerFactory.CreateLogger("AtomexApp"))
                .UseCurrenciesProvider(currenciesProvider)
                .UseSymbolsProvider(symbolsProvider)
                .UseCurrenciesUpdater(new CurrenciesUpdater(currenciesProvider))
                .UseSymbolsUpdater(new SymbolsUpdater(symbolsProvider))
                .UseQuotesProvider(quotesProvider);

            var mainWindow = new MainWindow();
            DialogService = new DialogService();

            NotificationsService = new NotificationsService(AtomexApp, mainWindow.NotificationManager);
            MainWindowViewModel = new MainWindowViewModel(AtomexApp, mainWindow);
            mainWindow.DataContext = MainWindowViewModel;
            desktop.Exit += OnExit;

            if (desktop.Args.Length != 0)
            {
                MainWindowViewModel.StartupData = desktop.Args[0];
                Log.Information("Setting startup data from start args {Data}", desktop.Args[0]);
            }

            desktop.MainWindow = mainWindow;
            AtomexApp.Start();

            _singleInstanceLoopbackService = new SingleInstanceLoopbackService();
            _singleInstanceLoopbackService.RunInBackground((receivedText) =>
            {
                MainWindowViewModel.StartupData = receivedText;
                Log.Information("Received startup data from socket {Data}", receivedText);
                _ = Dispatcher.UIThread.InvokeAsync(() => { desktop.MainWindow.Activate(); });
            });

            base.OnFrameworkInitializationCompleted();
        }
        
        void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            Log.Information("Application shutdown");

            try
            {
                AtomexApp.Stop();
                Environment.Exit(0);
            }
            catch (Exception)
            {
                Log.Error("Error stopping Atomex in OnExit");
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
            .FirstOrDefault(a => a.GetName().Name == "Atomex.Client.Core") ?? throw new Exception("Can't find core library assembly");

        private static string CurrenciesConfigurationString
        {
            get
            {
                const string resourceName = "currencies.json";
                var resourceNames = CoreAssembly.GetManifestResourceNames();
                var fullFileName = resourceNames.FirstOrDefault(n => n.EndsWith(resourceName));
                var stream = CoreAssembly.GetManifestResourceStream(fullFileName!);
                using StreamReader reader = new(stream!);
                return reader.ReadToEnd();
            }
        }

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
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? url : "open",
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"{url}" : "",
                    CreateNoWindow = true,
                    UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                });
            }
        }

        private static void ShellExec(string cmd, bool waitForExit = true)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            );

            if (waitForExit)
                process?.WaitForExit();
        }

        private void ConfigureLoggers()
        {
            // todo: remove Serilog static logger and use Serilog only as provider for Microsoft.Extensions.Logging

            // init Serilog static logger
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .ReadFrom.Configuration(Configuration)
#else
                .WriteTo.Sentry(o =>
                {
                    o.Dsn = "https://e6728d16ed934432b6dd6735df8bd37b@newsentry.baking-bad.org/3";
                    // Debug and higher are stored as breadcrumbs (default is Information)
                    o.MinimumBreadcrumbLevel = LogEventLevel.Information;
                    // Warning and higher is sent as event (default is Error)
                    o.MinimumEventLevel = LogEventLevel.Error;
                })
#endif
                .CreateLogger();

            // init Microsoft.Extensions.Logging logger factory
            LoggerFactory = new LoggerFactory()
                .AddSerilog();
        }
    }
}
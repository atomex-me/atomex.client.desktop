using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Sentry;

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
        public static IClipboard Clipboard;
        public static NotificationsService NotificationsService;
        public static ILoggerFactory LoggerFactory;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            TemplateService = new TemplateService();
            Clipboard = AvaloniaLocator.Current.GetService<IClipboard>();

            // set invariant culture by default
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // configure loggers
            ConfigureLoggers();

            var currenciesProvider = new CurrenciesProvider(CurrenciesConfigurationString);
            var symbolsProvider = new SymbolsProvider(SymbolsConfiguration);

            var bitfinexQuotesProvider = new BitfinexQuotesProvider(
                currencies: currenciesProvider
                    .GetCurrencies(Network.MainNet)
                    .Select(c => c.Name),
                baseCurrency: QuotesProvider.Usd,
                log: LoggerFactory.CreateLogger<BitfinexQuotesProvider>());

            var tezToolsQuotesProvider = new TezToolsQuotesProvider(
                log: LoggerFactory.CreateLogger<TezToolsQuotesProvider>());

            var quotesProvider = new MultiSourceQuotesProvider(
                log: LoggerFactory.CreateLogger<MultiSourceQuotesProvider>(),
                bitfinexQuotesProvider, tezToolsQuotesProvider);

            // init Atomex client app
            AtomexApp = new AtomexApp()
                .UseCurrenciesProvider(currenciesProvider)
                .UseSymbolsProvider(symbolsProvider)
                .UseCurrenciesUpdater(new CurrenciesUpdater(currenciesProvider))
                .UseSymbolsUpdater(new SymbolsUpdater(symbolsProvider))
                .UseQuotesProvider(quotesProvider);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow();
                DialogService = new DialogService();
                NotificationsService = new NotificationsService(AtomexApp, mainWindow.NotificationManager);

                var mainWindowViewModel = new MainWindowViewModel(AtomexApp, mainWindow);

                mainWindow.DataContext = mainWindowViewModel;
                desktop.MainWindow = mainWindow;
                desktop.Exit += OnExit;

                // var sink = new InMemorySink(mainWindowViewModel.LogEvent);
                // Log.Logger = new LoggerConfiguration()
                //     .WriteTo.Sink(sink)
                //     .CreateLogger();
            }

            AtomexApp.Start();

            base.OnFrameworkInitializationCompleted();
        }


        void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
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
            .FirstOrDefault(a => a.GetName().Name == "Atomex.Client.Core");

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

    class InMemorySink : ILogEventSink
    {
        private readonly Action<string> _logAction;

        public InMemorySink(Action<string> logAction)
        {
            _logAction = logAction;
        }

        readonly ITextFormatter _textFormatter = new MessageTemplateTextFormatter("[{Level}] {Message}{Exception}");

        public ConcurrentQueue<string> Events { get; } = new ConcurrentQueue<string>();

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
                throw new ArgumentNullException(nameof(logEvent));

            var renderSpace = new StringWriter();
            _textFormatter.Format(logEvent, renderSpace);

            Events.Enqueue(renderSpace.ToString());

            _logAction.Invoke(renderSpace.ToString());
        }
    }
}
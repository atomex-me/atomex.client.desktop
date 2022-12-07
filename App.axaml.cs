using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
using Avalonia.Threading;


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

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // set invariant culture by default
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            // configure loggers
            ConfigureLoggers();

            UrlsOpened += (sender, args) =>
            {
                if (args.Urls.Length == 0) return;

                MainWindowViewModel.StartupData = args.Urls[0];
                Log.Information("Setting startup data from URLOpened {Url}", args.Urls[0]);
            };

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                const int atomexTcpPort = 49531;
                var appAlreadyRunning = true;

                using var tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    tcpClient.Connect(IPAddress.Loopback, atomexTcpPort);
                }
                catch (SocketException)
                {
                    appAlreadyRunning = false;
                }

                if (appAlreadyRunning)
                {
                    if (desktop.Args.Length != 0)
                    {
                        var bytesArgs = Encoding.UTF8.GetBytes(desktop.Args[0]);
                        tcpClient.Send(bytesArgs);
                        Log.Information("Sending data to running app instance {Data}", desktop.Args[0]);
                    }

                    tcpClient.Disconnect(false);
                    tcpClient.Close();
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

                Task.Run(async () =>
                {
                    var ipPoint = new IPEndPoint(IPAddress.Loopback, atomexTcpPort);
                    using var serverSocket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);
                    serverSocket.Bind(ipPoint);
                    serverSocket.Listen();

                    while (true)
                    {
                        var clientSocket = await serverSocket.AcceptAsync();
                        var buffer = new List<byte>();

                        do
                        {
                            var currByte = new byte[1];
                            var byteCounter = clientSocket.Receive(currByte, currByte.Length, SocketFlags.None);
                            if (byteCounter.Equals(1))
                                buffer.Add(currByte[0]);
                        } while (clientSocket.Available != 0);

                        var receivedSocketText = Encoding.UTF8.GetString(buffer.ToArray(), 0, buffer.Count);
                        if (receivedSocketText != string.Empty)
                        {
                            MainWindowViewModel.StartupData = receivedSocketText;
                            Log.Information("Received startup data from socket {Data}", receivedSocketText);
                            _ = Dispatcher.UIThread.InvokeAsync(() => { desktop.MainWindow.Activate(); });
                        }

                        clientSocket.Disconnect(false);
                        clientSocket.Close();
                        clientSocket.Dispose();
                    }
                });

                // var sink = new InMemorySink(mainWindowViewModel.LogEvent);
                // Log.Logger = new LoggerConfiguration()
                //     .WriteTo.Sink(sink)
                //     .CreateLogger();
            }

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
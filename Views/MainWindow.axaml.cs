using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Atomex.Client.Desktop.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Markup.Xaml;
using System.Timers;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using Serilog;
using ILogger = NetSparkleUpdater.Interfaces.ILogger;
using Timer = System.Timers.Timer;


namespace Atomex.Client.Desktop.Views
{
    public class SparkleLogger : ILogger
    {
        public void PrintMessage(string message, params object[] arguments)
        {
            Log.Debug(string.Format(message, arguments.Select(x => x.ToString()).ToArray()));
        }
    }

    public class MainWindow : Window, IMainView
    {
        private Timer _activityTimer;
        private bool _inactivityControlEnabled;
        public event CancelEventHandler MainViewClosing;
        public event EventHandler Inactivity;

        private readonly string NETSPARKLE_PK = "76FH2gIo7D5mpPPfnard5C9cVwq8TFaxpo/Wi2Iem/E=";
        private SparkleUpdater _sparkle;
        private AppCastItem _lastUpdate;
        private bool _isOsx;
        private bool _isWin;
        private bool _isLinux;
        private string _appcastUrl;
        private string _updateDownloadPath;
        private IUpdater _atomexUpdater;

        private MainWindowViewModel ctx;

        public MainWindow()
        {
            InitializeComponent();
            this.AttachDevTools();

            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == Control.DataContextProperty)
                {
                    ctx = (MainWindowViewModel) e.NewValue!;

                    ctx.OnUpdateAction = ManualUpdate_Click;
                }
            };

            Closing += (sender, args) => MainViewClosing?.Invoke(sender, args);

            InputManager.Instance.PreProcess.OfType<RawInputEventArgs>()
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(
                    (_) =>
                    {
                        if (_inactivityControlEnabled && _activityTimer != null)
                        {
                            _activityTimer.Stop();
                            _activityTimer.Start();
                        }
                    });
            _isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            _isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            _appcastUrl =
                "https://github.com/atomex-me/atomex.client.desktop/releases/latest/download/appcast_{0}.xml";

            if (_isOsx) _appcastUrl = string.Format(_appcastUrl, "osx");
            if (_isWin) _appcastUrl = string.Format(_appcastUrl, "win");
            if (_isLinux) _appcastUrl = string.Format(_appcastUrl, "linux");

            _sparkle = new SparkleUpdater(
                _appcastUrl,
                new Ed25519Checker(SecurityMode.OnlyVerifySoftwareDownloads,
                    NETSPARKLE_PK))
            {
                UserInteractionMode = UserInteractionMode.DownloadNoInstall
            };

            _sparkle.LogWriter = new SparkleLogger();
            _sparkle.SecurityProtocolType = System.Net.SecurityProtocolType.Tls12;
            _sparkle.StartLoop(false, false);

            CheckForUpdates(null, null);
            var checkUpdateReadyTimer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
            checkUpdateReadyTimer.AutoReset = true;
            checkUpdateReadyTimer.Elapsed += CheckForUpdates;
            checkUpdateReadyTimer.Start();


            _sparkle.DownloadStarted += (item, path) => { Console.WriteLine($"Updating download started {path}"); };
            _sparkle.DownloadFinished += (item, path) =>
            {
                Log.Information($"Updating download finished ${path}");
                ctx.UpdateDownloadProgress = 100;
                _updateDownloadPath = path;
            };

            _sparkle.DownloadMadeProgress += (sender, item, args) =>
            {
                ctx.UpdateDownloadProgress =
                    (int) ((double) args.BytesReceived / (double) args.TotalBytesToReceive * 100);
            };
        }

        private void ManualUpdate_Click()
        {
            if (_lastUpdate != null)
            {
                if (_isOsx)
                {
                    _atomexUpdater.InstallUpdate(_lastUpdate);
                    return;
                }

                if (_isWin || _isLinux)
                {
                    _atomexUpdater.InstallUpdate(_lastUpdate, _updateDownloadPath);
                }
            }
        }

        public void StartInactivityControl(TimeSpan timeOut)
        {
            _activityTimer = new Timer(timeOut.TotalMilliseconds) {AutoReset = true, Enabled = true};
            _activityTimer.Elapsed += InactivityTimerElapsed;

            _inactivityControlEnabled = true;
        }

        private void InactivityTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Inactivity?.Invoke(sender, null);
        }

        public void StopInactivityControl()
        {
            _inactivityControlEnabled = false;
            _activityTimer?.Stop();
        }

        private void CheckForUpdates(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Task.Run(async () =>
            {
                var _updateInfo = await _sparkle.CheckForUpdatesQuietly();
                Log.Information($"Update info is {_updateInfo.Status}");
                if (_updateInfo.Status == UpdateStatus.UpdateAvailable)
                {
                    try
                    {
                        _lastUpdate = _updateInfo.Updates.Last();
                        ctx.HasUpdates = true;
                        ctx.UpdateVersion = _lastUpdate.Version;
                        await _sparkle.InitAndBeginDownload(_lastUpdate);

                        if (_atomexUpdater != null)
                            return;

                        if (_isOsx)
                            _atomexUpdater = new MacUpdater
                            {
                                SignatureVerifier = _sparkle.SignatureVerifier,
                                UpdateDownloader = _sparkle.UpdateDownloader
                            };

                        if (_isWin)
                            _atomexUpdater = new WindowsUpdater(_appcastUrl,
                                new Ed25519Checker(SecurityMode.OnlyVerifySoftwareDownloads,
                                    NETSPARKLE_PK))
                            {
                                SignatureVerifier = _sparkle.SignatureVerifier,
                                UpdateDownloader = _sparkle.UpdateDownloader
                            };

                        if (_isLinux)
                            _atomexUpdater = new LinuxUpdater()
                            {
                                SignatureVerifier = _sparkle.SignatureVerifier,
                                UpdateDownloader = _sparkle.UpdateDownloader
                            };
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                }
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
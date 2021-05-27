using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Globalization;
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
using Avalonia.Interactivity;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.Interfaces;
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

        private SparkleUpdater _sparkle;

        private AppCastItem LastUpdate;

        private MacUpdater MacUpdater;

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

            _sparkle = new SparkleUpdater(
                "https://github.com/atomex-me/atomex.client.desktop/releases/latest/download/appcast.xml",
                new Ed25519Checker(SecurityMode.OnlyVerifySoftwareDownloads,
                    "76FH2gIo7D5mpPPfnard5C9cVwq8TFaxpo/Wi2Iem/E="))
            {
                UserInteractionMode = UserInteractionMode.DownloadNoInstall
            };
            _sparkle.LogWriter = new SparkleLogger();
            _sparkle.SecurityProtocolType = System.Net.SecurityProtocolType.Tls12;
            _sparkle.StartLoop(false, false);

            CheckForUpdates(null, null);
            var checkUpdateReadyTimer = new Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
            checkUpdateReadyTimer.AutoReset = true;
            checkUpdateReadyTimer.Elapsed += CheckForUpdates;
            checkUpdateReadyTimer.Start();

            _sparkle.DownloadStarted += (item, path) => { Console.WriteLine($"Updating download started {path}"); };
            _sparkle.DownloadFinished += (item, path) =>
            {
                Console.WriteLine($"Updating download finished ${path}");
                ctx.UpdateDownloadProgress = 100;
            };

            _sparkle.DownloadMadeProgress += (sender, item, args) =>
            {
                ctx.UpdateDownloadProgress =
                    (int) ((double) args.BytesReceived / (double) args.TotalBytesToReceive * 100);
            };
        }

        private void ManualUpdate_Click()
        {
            if (LastUpdate != null)
            {
                MacUpdater.InstallUpdate(LastUpdate);
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
                        LastUpdate = _updateInfo.Updates.Last();
                        ctx.HasUpdates = true;
                        ctx.UpdateVersion = LastUpdate.Version;
                        await _sparkle.InitAndBeginDownload(LastUpdate);

                        if (MacUpdater == null)
                        {
                            MacUpdater = new MacUpdater
                            {
                                SignatureVerifier = _sparkle.SignatureVerifier,
                                UpdateDownloader = _sparkle.UpdateDownloader
                            };
                        }
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
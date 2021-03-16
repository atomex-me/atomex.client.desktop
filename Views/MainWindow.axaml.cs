using System;
using System.ComponentModel;
using System.Reactive.Linq;
using Atomex.Client.Desktop.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace Atomex.Client.Desktop.Views
{
    public class MainWindow : Window, IMainView
    {
        private Timer _activityTimer;
        private bool _inactivityControlEnabled;

        public event CancelEventHandler MainViewClosing;
        public event EventHandler Inactivity;

        public MainWindow()
        {
            InitializeComponent();
            this.AttachDevTools();

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
            
        }

        public void StartInactivityControl(TimeSpan timeOut)
        {
            

            _activityTimer = new Timer(TimeSpan.FromSeconds(5).TotalMilliseconds) {AutoReset = true, Enabled = true};
            _activityTimer.Elapsed += inactivityTimerElapsed;

            _inactivityControlEnabled = true;
        }

        private void inactivityTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Inactivity?.Invoke(sender, null);
        }

        public void StopInactivityControl()
        {
            _inactivityControlEnabled = false;
            _activityTimer?.Stop();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
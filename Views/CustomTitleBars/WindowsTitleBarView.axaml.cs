using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Atomex.Client.Desktop.Common;

namespace Atomex.Client.Desktop.Views.CustomTitleBars
{
    public class WindowsTitleBarView : UserControl
    {
        private Button closeButton;
        private Button minimizeButton;
        private Button zoomButton;

        private DockPanel titleBarBackground;
        private StackPanel titleAndWindowIconWrapper;

        public static readonly StyledProperty<bool> IsSeamlessProperty =
            AvaloniaProperty.Register<MacosTitleBarView, bool>(nameof(IsSeamless));

        public bool IsSeamless
        {
            get { return GetValue(IsSeamlessProperty); }
            set
            {
                SetValue(IsSeamlessProperty, value);
                if (titleBarBackground != null && titleAndWindowIconWrapper != null)
                {
                    titleBarBackground.IsVisible = IsSeamless ? false : true;
                    titleAndWindowIconWrapper.IsVisible = IsSeamless ? false : true;
                }
            }
        }

        public WindowsTitleBarView()
        {
            this.InitializeComponent();
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                this.IsVisible = false;
            }
            else
            {
                minimizeButton = this.FindControl<Button>("MinimizeButton");
                zoomButton = this.FindControl<Button>("MaximizeButton");
                closeButton = this.FindControl<Button>("CloseButton");

                minimizeButton.Click += MinimizeWindow;
                zoomButton.Click += MaximizeWindow;
                closeButton.Click += CloseWindow;

                titleBarBackground = this.FindControl<DockPanel>("TitleBarBackground");
                titleAndWindowIconWrapper = this.FindControl<StackPanel>("TitleAndWindowIconWrapper");

                SubscribeToWindowState();
            }
        }

        private void CloseWindow(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Window hostWindow = (Window) this.VisualRoot;
            hostWindow.Close();
        }

        private void MaximizeWindow(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Window hostWindow = (Window) this.VisualRoot;

            if (hostWindow.WindowState == WindowState.Normal)
            {
                hostWindow.WindowState = WindowState.Maximized;
            }
            else
            {
                hostWindow.WindowState = WindowState.Normal;
            }
        }

        private void MinimizeWindow(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Window hostWindow = (Window) this.VisualRoot;
            hostWindow.WindowState = WindowState.Minimized;
        }

        private async void SubscribeToWindowState()
        {
            Window hostWindow = (Window) this.VisualRoot;

            while (hostWindow == null)
            {
                hostWindow = (Window) this.VisualRoot;
                await Task.Delay(50);
            }

            hostWindow.ExtendClientAreaTitleBarHeightHint = 30;
            hostWindow.GetObservable(Window.WindowStateProperty).SubscribeInMainThread(s =>
            {
                hostWindow.SystemDecorations = SystemDecorations.None;
                
                if (s != WindowState.Maximized)
                {
                    hostWindow.Padding = new Thickness(0, 0, 0, 0);
                }
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
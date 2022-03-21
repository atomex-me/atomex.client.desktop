using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views.SendViews
{
    public class EthereumSendView : UserControl
    {
        public EthereumSendView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void HelpClickHandler(object sender, RoutedEventArgs e)
        {
            var source = e.Source as Button;

            source?.SetValue(ToolTip.IsOpenProperty, true);
        }
    }
}
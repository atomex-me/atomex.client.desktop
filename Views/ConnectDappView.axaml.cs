using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views
{
    public partial class ConnectDappView : UserControl
    {
        public ConnectDappView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
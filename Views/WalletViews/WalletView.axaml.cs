using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views.WalletViews
{
    public class WalletView : UserControl
    {
        public WalletView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
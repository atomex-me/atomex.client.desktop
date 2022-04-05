using Atomex.Client.Desktop.ViewModels.WalletViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Views.WalletViews
{
    public class TezosTokensWalletView : UserControl
    {
        public TezosTokensWalletView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
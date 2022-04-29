using Atomex.Client.Desktop.ViewModels.WalletViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Views.WalletViews
{
    public class TezosTokensWalletView : UserControl
    {
        public TezosTokensWalletView()
        {
            InitializeComponent();
            
#if DEBUG
            if (!Design.IsDesignMode) return;

            var designGrid = this.FindControl<Grid>("DesignGrid");
            designGrid.Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x21, 0x39));
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
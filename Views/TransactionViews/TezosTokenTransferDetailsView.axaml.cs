using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views.TransactionViews
{
    public class TezosTokenTransferDetailsView : UserControl
    {
        public TezosTokenTransferDetailsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
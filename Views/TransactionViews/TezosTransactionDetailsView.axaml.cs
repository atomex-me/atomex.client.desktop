using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Atomex.Client.Desktop.Views.TransactionViews
{
    public class TezosTransactionDetailsView : UserControl
    {
        public TezosTransactionDetailsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
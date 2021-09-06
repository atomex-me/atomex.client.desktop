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
            
            var dgTransactions = this.FindControl<DataGrid>("DgTransactions");
            
            dgTransactions.CellPointerPressed += (sender, args) =>
            {
                var cellIndex = args.Row.GetIndex();
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ((TezosTokensWalletViewModel) DataContext!).CellPointerPressed(cellIndex);
                });
            };

            dgTransactions.Sorting += (sender, args) =>
            {
                ((TezosTokensWalletViewModel) DataContext!).SortInfo = args.Column.Header.ToString();
                args.Handled = true;
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
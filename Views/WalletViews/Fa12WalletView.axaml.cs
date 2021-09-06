using Atomex.Client.Desktop.ViewModels.WalletViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Views.WalletViews
{
    public class Fa12WalletView : UserControl
    {
        public Fa12WalletView()
        {
            InitializeComponent();
            
            var dgTransactions = this.FindControl<DataGrid>("DgTransactions");
            
            dgTransactions.CellPointerPressed += (sender, args) =>
            {
                var cellIndex = args.Row.GetIndex();
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ((Fa12WalletViewModel) DataContext!).CellPointerPressed(cellIndex);
                });
            };

            dgTransactions.Sorting += (sender, args) =>
            {
                ((Fa12WalletViewModel) DataContext!).SortInfo = args.Column.Header.ToString();
                args.Handled = true;
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
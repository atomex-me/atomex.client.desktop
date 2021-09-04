using System;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Client.Desktop.ViewModels.WalletViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Views.WalletViews
{
    public class WalletView : UserControl
    {
        public WalletView()
        {
            InitializeComponent();

            var dgTransactions = this.FindControl<DataGrid>("DgTransactions");
            
            dgTransactions.CellPointerPressed += (sender, args) =>
            {
                var cellIndex = args.Row.GetIndex();
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ((WalletViewModel) DataContext!).CellPointerPressed(cellIndex);
                });
            };

            dgTransactions.Sorting += (sender, args) =>
            {
                ((WalletViewModel) DataContext!).SortInfo = args.Column.Header.ToString();
                args.Handled = true;
            };
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
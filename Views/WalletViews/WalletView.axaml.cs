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
using Avalonia.Media;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Views.WalletViews
{
    public class WalletView : UserControl
    {
        public WalletView()
        {
            InitializeComponent();

            var dgTransactions = this.FindControl<DataGrid>("DgTransactions");
            if (dgTransactions != null)
            {
                dgTransactions.CellPointerPressed += (sender, args) =>
                {
                    var cellIndex = args.Row.GetIndex();
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ((WalletViewModel)DataContext!).CellPointerPressed(cellIndex);
                    });
                };

                dgTransactions.Sorting += (sender, args) =>
                {
                    ((WalletViewModel)DataContext!).SortInfo = args.Column.Header.ToString();
                    args.Handled = true;
                };
            }
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
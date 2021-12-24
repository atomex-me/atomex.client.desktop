using System;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Atomex.Client.Desktop.ViewModels;

namespace Atomex.Client.Desktop.Views
{
    public class ConversionView : UserControl
    {
        public ConversionView()
        {
            InitializeComponent();

            var amountStringTextBox = this.FindControl<TextBox>("AmountString");

            amountStringTextBox.GetObservable(TextBox.TextProperty)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Where(text => text != null)
                .Subscribe(text =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (DataContext is ConversionViewModel viewModel)
                            viewModel.AmountString = text;
                    });
                });

            var dgConversions = this.FindControl<DataGrid>("DgConversions");

            dgConversions.CellPointerPressed += (sender, args) =>
            {
                var cellIndex = args.Row.GetIndex();
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (DataContext is ConversionViewModel viewModel)
                        viewModel.CellPointerPressed(cellIndex);
                });
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
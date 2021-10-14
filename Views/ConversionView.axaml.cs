using System;
using System.Reactive.Linq;
using Atomex.Client.Desktop.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Serilog;

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
                    ((ConversionViewModel) DataContext!).CellPointerPressed(cellIndex);
                });
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void DgConversions_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            Log.Fatal(e.ToString());
        }
    }
}
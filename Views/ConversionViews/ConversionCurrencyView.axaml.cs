using System;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using Atomex.Client.Desktop.ViewModels;

namespace Atomex.Client.Desktop.Views
{
    public partial class ConversionCurrencyView : UserControl
    {
        public ConversionCurrencyView()
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
                        if (DataContext is ConversionCurrencyViewModel viewModel)
                            viewModel.AmountString = text;
                    });
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnGotFocus(object sender, GotFocusEventArgs args)
        {
            if (DataContext is ConversionCurrencyViewModel viewModel)
                viewModel.RaiseGotInputFocus();
        }
    }
}
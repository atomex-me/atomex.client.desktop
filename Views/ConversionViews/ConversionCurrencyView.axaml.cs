using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

using Atomex.Client.Desktop.ViewModels;

namespace Atomex.Client.Desktop.Views
{
    public partial class ConversionCurrencyView : UserControl
    {
        public ConversionCurrencyView()
        {
            InitializeComponent();

            var amountStringTextBox = this.FindControl<TextBox>("AmountString");

            amountStringTextBox.AddHandler(KeyDownEvent, (_, args) =>
            {
                if (DataContext is not ConversionCurrencyViewModel conversionCurrencyViewModel ||
                    args.Key is not (Key.Back or Key.Delete)) return;

                if (amountStringTextBox.SelectionStart != amountStringTextBox.SelectionEnd)
                {
                    conversionCurrencyViewModel.SetAmountFromString(0m.ToString(CultureInfo.InvariantCulture));
                    args.Handled = true;
                    return;
                }

                var dotSymbol = conversionCurrencyViewModel.AmountString.FirstOrDefault(c => !char.IsDigit(c));
                var dotIndex = conversionCurrencyViewModel.AmountString.IndexOf(dotSymbol);
                if (dotIndex != amountStringTextBox.CaretIndex - 1) return;
                amountStringTextBox.CaretIndex -= 1;
            }, RoutingStrategies.Tunnel);

            amountStringTextBox.GetObservable(TextBox.TextProperty)
                .Where(_ => amountStringTextBox.SelectionStart == amountStringTextBox.SelectionEnd)
                .Subscribe(text =>
                {
                    if (DataContext is not ConversionCurrencyViewModel conversionCurrencyViewModel) return;
                    conversionCurrencyViewModel.SetAmountFromString(text);
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
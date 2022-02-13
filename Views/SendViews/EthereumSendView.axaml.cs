using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Atomex.Client.Desktop.ViewModels.SendViewModels;

namespace Atomex.Client.Desktop.Views.SendViews
{
    public class EthereumSendView : UserControl
    {
        public EthereumSendView()
        {
            InitializeComponent();

            var amountStringTextBox = this.FindControl<TextBox>("AmountString");
            var gasPriceStringTextBox = this.FindControl<TextBox>("GasPriceString");

            amountStringTextBox.AddHandler(KeyDownEvent, (_, args) =>
            {
                if (DataContext is not EthereumSendViewModel sendViewModel ||
                    args.Key is not (Key.Back or Key.Delete)) return;

                if (amountStringTextBox.SelectionStart != amountStringTextBox.SelectionEnd)
                {
                    sendViewModel.SetAmountFromString(0m.ToString(CultureInfo.InvariantCulture));
                    args.Handled = true;
                    return;
                }

                var dotSymbol = sendViewModel.AmountString.FirstOrDefault(c => !char.IsDigit(c));
                var dotIndex = sendViewModel.AmountString.IndexOf(dotSymbol);
                if (dotIndex != amountStringTextBox.CaretIndex - 1) return;
                amountStringTextBox.CaretIndex -= 1;
            }, RoutingStrategies.Tunnel);

            gasPriceStringTextBox.AddHandler(KeyDownEvent, (_, args) =>
            {
                if (DataContext is not EthereumSendViewModel sendViewModel ||
                    args.Key is not (Key.Back or Key.Delete)) return;

                if (gasPriceStringTextBox.SelectionStart == gasPriceStringTextBox.SelectionEnd) return;
                sendViewModel.SetGasPriceFromString(0m.ToString(CultureInfo.InvariantCulture));
                args.Handled = true;
            }, RoutingStrategies.Tunnel);

            amountStringTextBox.GetObservable(TextBox.TextProperty)
                .Where(_ => amountStringTextBox.SelectionStart == amountStringTextBox.SelectionEnd)
                .Subscribe(text =>
                {
                    if (DataContext is not EthereumSendViewModel sendViewModel) return;
                    sendViewModel.SetAmountFromString(text);
                });

            gasPriceStringTextBox.GetObservable(TextBox.TextProperty)
                .Where(_ => gasPriceStringTextBox.SelectionStart == gasPriceStringTextBox.SelectionEnd)
                .Subscribe(text =>
                {
                    if (DataContext is not EthereumSendViewModel sendViewModel) return;
                    sendViewModel.SetGasPriceFromString(text);
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void HelpClickHandler(object sender, RoutedEventArgs e)
        {
            var source = e.Source as Button;

            source?.SetValue(ToolTip.IsOpenProperty, true);
        }
    }
}
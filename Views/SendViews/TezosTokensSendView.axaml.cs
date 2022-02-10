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
    public class TezosTokensSendView : UserControl
    {
        public TezosTokensSendView()
        {
            InitializeComponent();
            
            var amountStringTextBox = this.FindControl<TextBox>("AmountString");
            var feeStringTextBox = this.FindControl<TextBox>("FeeString");

            amountStringTextBox.AddHandler(KeyDownEvent, (_, args) =>
            {
                if (DataContext is not TezosTokensSendViewModel sendViewModel ||
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

            feeStringTextBox.AddHandler(KeyDownEvent, (_, args) =>
            {
                if (DataContext is not TezosTokensSendViewModel sendViewModel ||
                    args.Key is not (Key.Back or Key.Delete)) return;

                if (feeStringTextBox.SelectionStart != feeStringTextBox.SelectionEnd)
                {
                    sendViewModel.SetFeeFromString(0m.ToString(CultureInfo.InvariantCulture));
                    args.Handled = true;
                    return;
                }

                var dotSymbol = sendViewModel.FeeString.FirstOrDefault(c => !char.IsDigit(c));
                var dotIndex = sendViewModel.FeeString.IndexOf(dotSymbol);
                if (dotIndex != feeStringTextBox.CaretIndex - 1) return;
                feeStringTextBox.CaretIndex -= 1;
            }, RoutingStrategies.Tunnel);

            amountStringTextBox.GetObservable(TextBox.TextProperty)
                .Where(_ => amountStringTextBox.SelectionStart == amountStringTextBox.SelectionEnd)
                .Subscribe(text =>
                {
                    if (DataContext is not TezosTokensSendViewModel sendViewModel) return;
                    sendViewModel.SetAmountFromString(text);
                });

            feeStringTextBox.GetObservable(TextBox.TextProperty)
                .Where(_ => feeStringTextBox.SelectionStart == feeStringTextBox.SelectionEnd)
                .Subscribe(text =>
                {
                    if (DataContext is not TezosTokensSendViewModel sendViewModel) return;
                    sendViewModel.SetFeeFromString(text);
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
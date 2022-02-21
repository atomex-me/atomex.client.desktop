using System.Globalization;
using System.Linq;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.Client.Desktop.Common;

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
                    sendViewModel.SetAmountFromString(0.ToString(CultureInfo.CurrentCulture));
                    args.Handled = true;
                    return;
                }

                var dotSymbol = sendViewModel.AmountString.FirstOrDefault(c => !char.IsDigit(c));
                var dotIndex = sendViewModel.AmountString.IndexOf(dotSymbol);
                switch (args.Key)
                {
                    case Key.Back when dotIndex != amountStringTextBox.CaretIndex - 1:
                        return;
                    case Key.Back:
                        amountStringTextBox.CaretIndex -= 1;
                        break;
                    case Key.Delete when dotIndex != amountStringTextBox.CaretIndex:
                        return;
                    case Key.Delete:
                        amountStringTextBox.CaretIndex += 1;
                        break;
                }
            }, RoutingStrategies.Tunnel);

            feeStringTextBox.AddHandler(KeyDownEvent, (_, args) =>
            {
                if (DataContext is not TezosTokensSendViewModel sendViewModel ||
                    args.Key is not (Key.Back or Key.Delete)) return;

                if (feeStringTextBox.SelectionStart != feeStringTextBox.SelectionEnd)
                {
                    sendViewModel.SetFeeFromString(0.ToString(CultureInfo.CurrentCulture));
                    args.Handled = true;
                    return;
                }

                var dotSymbol = sendViewModel.FeeString.FirstOrDefault(c => !char.IsDigit(c));
                var dotIndex = sendViewModel.FeeString.IndexOf(dotSymbol);
                switch (args.Key)
                {
                    case Key.Back when dotIndex != amountStringTextBox.CaretIndex - 1:
                        return;
                    case Key.Back:
                        amountStringTextBox.CaretIndex -= 1;
                        break;
                    case Key.Delete when dotIndex != amountStringTextBox.CaretIndex:
                        return;
                    case Key.Delete:
                        amountStringTextBox.CaretIndex += 1;
                        break;
                }
            }, RoutingStrategies.Tunnel);

            amountStringTextBox.GetObservable(TextBox.TextProperty)
                .Where(_ => amountStringTextBox.SelectionStart == amountStringTextBox.SelectionEnd)
                .SubscribeInMainThread(text =>
                {
                    if (DataContext is not TezosTokensSendViewModel sendViewModel) return;
                    sendViewModel.SetAmountFromString(text);
                });

            feeStringTextBox.GetObservable(TextBox.TextProperty)
                .Where(_ => feeStringTextBox.SelectionStart == feeStringTextBox.SelectionEnd)
                .SubscribeInMainThread(text =>
                {
                    if (DataContext is not TezosTokensSendViewModel sendViewModel) return;
                    sendViewModel.SetFeeFromString(text);
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
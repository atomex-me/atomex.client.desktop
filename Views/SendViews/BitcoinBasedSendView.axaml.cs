using System;
using System.Linq;
using System.Reactive.Linq;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ReactiveUI;


namespace Atomex.Client.Desktop.Views.SendViews
{
    public class BitcoinBasedSendView : UserControl
    {
        public BitcoinBasedSendView()
        {
            InitializeComponent();

            var amountStringTextBox = this.FindControl<TextBox>("AmountString");
            var feeStringTextBox = this.FindControl<TextBox>("FeeString");

            amountStringTextBox.AddHandler(KeyDownEvent, (_, args) =>
            {
                if (DataContext is not BitcoinBasedSendViewModel sendViewModel || args.Key != Key.Back) return;
                var dotSymbol = sendViewModel.AmountString.FirstOrDefault(c => !char.IsDigit(c));
                var dotIndex = sendViewModel.AmountString.IndexOf(dotSymbol);
                if (dotIndex != amountStringTextBox.CaretIndex - 1) return;
                amountStringTextBox.CaretIndex = dotIndex;
            }, RoutingStrategies.Tunnel);

            feeStringTextBox.AddHandler(KeyDownEvent, (_, args) =>
            {
                if (DataContext is not BitcoinBasedSendViewModel sendViewModel || args.Key != Key.Back) return;
                var dotSymbol = sendViewModel.FeeString.FirstOrDefault(c => !char.IsDigit(c));
                var dotIndex = sendViewModel.FeeString.IndexOf(dotSymbol);
                if (dotIndex != feeStringTextBox.CaretIndex - 1) return;
                feeStringTextBox.CaretIndex = dotIndex;
            }, RoutingStrategies.Tunnel);

            amountStringTextBox.GetObservable(TextBox.TextProperty)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(text =>
                {
                    if (DataContext is not BitcoinBasedSendViewModel sendViewModel) return;
                    sendViewModel.SetAmountFromString(text);
                });

            feeStringTextBox.GetObservable(TextBox.TextProperty)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(text =>
                {
                    if (DataContext is not BitcoinBasedSendViewModel sendViewModel) return;
                    sendViewModel.SetFeeFromString(text);
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
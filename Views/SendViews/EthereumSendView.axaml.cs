using System;
using System.Reactive.Linq;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Views.SendViews
{
    public class EthereumSendView : UserControl
    {
        public EthereumSendView()
        {
            InitializeComponent();
            
            var amountStringTextBox = this.FindControl<TextBox>("AmountString");
            var feePriceTextBox = this.FindControl<TextBox>("FeePriceString");
            var gasStringTextBox = this.FindControl<TextBox>("GasString");

            amountStringTextBox.GetObservable(TextBox.TextProperty)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(text =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (DataContext is EthereumSendViewModel sendViewModel)
                            sendViewModel.AmountString = text;
                    });
                });
            
            feePriceTextBox.GetObservable(TextBox.TextProperty)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(text =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (DataContext is EthereumSendViewModel sendViewModel)
                            sendViewModel.FeePriceString = text;
                    });
                });
            
            gasStringTextBox.GetObservable(TextBox.TextProperty)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(text =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (DataContext is EthereumSendViewModel sendViewModel)
                            sendViewModel.GasString = text;
                    });
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
using System;
using System.Reactive.Linq;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Views.SendViews
{
    public class BitcoinBasedSendView : UserControl
    {
        public BitcoinBasedSendView()
        {
            InitializeComponent();

            var amountStringTextBox = this.FindControl<TextBox>("AmountString");
            var feeStringTextBox = this.FindControl<TextBox>("FeeString");

            amountStringTextBox.GetObservable(TextBox.TextProperty)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(text =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (DataContext is BitcoinBasedSendViewModel sendViewModel)
                            sendViewModel.AmountString = text;
                    });
                });
            
            feeStringTextBox.GetObservable(TextBox.TextProperty)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(text =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (DataContext is BitcoinBasedSendViewModel sendViewModel)
                            sendViewModel.FeeString = text;
                    });
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
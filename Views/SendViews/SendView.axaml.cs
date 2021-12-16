using System;
using System.Reactive.Linq;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Views.SendViews
{
    public class SendView : UserControl
    {
        public SendView()
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
                        if (DataContext is SendViewModel sendViewModel)
                            sendViewModel.SetAmountFromString(text);
                    });
                });

            feeStringTextBox.GetObservable(TextBox.TextProperty)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(text =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (DataContext is SendViewModel sendViewModel)
                            sendViewModel.SetFeeFromString(text);
                    });
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
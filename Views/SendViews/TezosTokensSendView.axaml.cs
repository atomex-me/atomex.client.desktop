using System;
using System.Reactive.Linq;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Views.SendViews
{
    public class TezosTokensSendView : UserControl
    {
        public TezosTokensSendView()
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
                        ((TezosTokensSendViewModel) DataContext)!.AmountString = text;
                    });
                });
            
            feeStringTextBox.GetObservable(TextBox.TextProperty)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(text =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ((TezosTokensSendViewModel) DataContext)!.FeeString = text;
                    });
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
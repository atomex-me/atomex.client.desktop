using System;
using System.Reactive.Linq;
using Atomex.Client.Desktop.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Views
{
    public class WertCurrencyView : UserControl
    {
        public WertCurrencyView()
        {
            InitializeComponent();
            
            var fromAmountTextBox = this.FindControl<TextBox>("FromAmount");

            fromAmountTextBox.GetObservable(TextBox.TextProperty)
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Subscribe(text =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ((WertCurrencyViewModel) DataContext)!.FromAmountString = text;
                    });
                });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
using System;
using System.Reactive.Linq;
using Atomex.Client.Desktop.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Atomex.Client.Desktop.Views
{
    public class WertCurrencyView : UserControl
    {
        public WertCurrencyView()
        {
            InitializeComponent();
            
            // var fromAmountTextBox = this.FindControl<TextBox>("FromAmount");
            // var toAmountTextBox = this.FindControl<TextBox>("ToAmount");
            //
            // fromAmountTextBox.GetObservable(TextBox.TextProperty)
            //     .Throttle(TimeSpan.FromMilliseconds(1))
            //     .Skip(1)
            //     .Subscribe(text =>
            //     {
            //         Dispatcher.UIThread.InvokeAsync(() =>
            //         {
            //             ((WertCurrencyViewModel) DataContext)!.FromAmountString = text;
            //         });
            //     });
            //
            // toAmountTextBox.GetObservable(TextBox.TextProperty)
            //     .Throttle(TimeSpan.FromMilliseconds(1))
            //     .Skip(1)
            //     .Subscribe(text =>
            //     {
            //         Dispatcher.UIThread.InvokeAsync(() =>
            //         {
            //             ((WertCurrencyViewModel) DataContext)!.ToAmountString = text;
            //         });
            //     });
            //
            // fromAmountTextBox.AddHandler(KeyDownEvent, fromAmountKeyDown, RoutingStrategies.Tunnel);
            // void fromAmountKeyDown(object sender, KeyEventArgs e)
            // {
            //     ((WertCurrencyViewModel) DataContext)!.FromAmountChangedFromKeyboard = true;
            //     ((WertCurrencyViewModel) DataContext)!.StartAsyncRatesCheck(WertCurrencyViewModel.Side.From);
            // }
            //
            // toAmountTextBox.AddHandler(KeyDownEvent, toAmountKeyDown, RoutingStrategies.Tunnel);
            // void toAmountKeyDown(object sender, KeyEventArgs e)
            // {
            //     ((WertCurrencyViewModel) DataContext)!.ToAmountChangedFromKeyboard = true;
            //     ((WertCurrencyViewModel) DataContext)!.StartAsyncRatesCheck(WertCurrencyViewModel.Side.To);
            // }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
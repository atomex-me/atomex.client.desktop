using System;
using Atomex.Client.Desktop.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Serilog;

namespace Atomex.Client.Desktop.Views
{
    public class WalletMainView : UserControl
    {
        public WalletMainView()
        {
            InitializeComponent();

            var walletContentGrid = this.FindControl<Grid>("WalletContentGrid");
            
            walletContentGrid.AddHandler(PointerPressedEvent, handler!, RoutingStrategies.Tunnel);
            
            void handler(object sender, PointerPressedEventArgs e)
            {
                // if (DataContext is WertCurrencyViewModel viewModel)
                // {
                //     viewModel.FromAmountChangedFromKeyboard = true;
                //     viewModel.StartAsyncRatesCheck(WertCurrencyViewModel.Side.From);
                // }
                
                //e.Source.InteractiveParent.InteractiveParent.InteractiveParent.InteractiveParent
                
                Log.Fatal($"ShouldClose {GetShouldClose(e.Source)}");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private bool GetShouldClose(IInteractive parent)
        {
            var control = parent as Control;
            if (control is WalletMainView) return true;
            return !control.Classes.Contains("NoCloseRightPopup") && GetShouldClose(parent.InteractiveParent);
        }

        private void WalletContentGrid_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            Log.Fatal("PRESSED");
        }
    }
}
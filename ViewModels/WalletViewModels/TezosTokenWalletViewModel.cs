using System;
using System.Reactive.Linq;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.ViewModels;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosTokenWalletViewModel : WalletViewModel
    {
        [Reactive] public TezosTokenViewModel? TokenViewModel { get; set; }
        public string TokenFormat =>
            $"F{Math.Min(TokenViewModel?.TokenBalance.Decimals ?? AddressesHelper.MaxTokenCurrencyFormatDecimals, AddressesHelper.MaxTokenCurrencyFormatDecimals)}";

        public TezosTokenWalletViewModel(
            IAtomexApp app,
            Action<ViewModelBase?> showRightPopupContent) :
            base(app: app,
                showRightPopupContent: showRightPopupContent,
                setConversionTab: null,
                setWertCurrency: null,
                currency: null)
        {
            this.WhenAnyValue(vm => vm.TokenViewModel)
                .WhereNotNull()
                .Select(tokenViewModel => tokenViewModel.TokenBalance.Name)
                .SubscribeInMainThread(header => Header = header);
        }

        protected override void SubscribeToServices()
        {
            Log.Fatal("SubscribeToServices in TezosTokenWalletViewModel");
        }

        public TezosTokenWalletViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

#if DEBUG
        protected override void DesignerMode()
        {
            base.DesignerMode();
        }
#endif
    }
}
using System;
using System.Reactive.Linq;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.SendViewModels;
using Atomex.ViewModels;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosTokenWalletViewModel : WalletViewModel
    {
        private const int MaxBalanceDecimals = AddressesHelper.MaxTokenCurrencyFormatDecimals;

        public string TokenFormat =>
            TokenViewModel?.TokenBalance != null && TokenViewModel.TokenBalance.Decimals != 0
                ? $"F{Math.Min(TokenViewModel.TokenBalance.Decimals, MaxBalanceDecimals)}"
                : $"F{MaxBalanceDecimals}";

        [Reactive] public TezosTokenViewModel? TokenViewModel { get; set; }

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

        protected override void OnReceiveClick()
        {
            if (TokenViewModel?.Contract == null) return;

            var tezosConfig = _app.Account
                .Currencies
                .GetByName(TezosConfig.Xtz);

            var receiveViewModel = new ReceiveViewModel(
                app: _app,
                currency: tezosConfig,
                tokenContract: TokenViewModel.Contract.Address,
                tokenType: TokenViewModel.Contract.GetContractType());

            App.DialogService.Show(receiveViewModel);
        }

        protected override void OnSendClick()
        {
            if (TokenViewModel?.Contract.Address == null)
                return;

            var sendViewModel = new TezosTokensSendViewModel(
                app: _app,
                tokenContract: TokenViewModel.Contract.Address,
                tokenId: TokenViewModel.TokenBalance.TokenId,
                tokenType: TokenViewModel.Contract.GetContractType(),
                tokenPreview: TokenViewModel.TokenPreview,
                from: null);

            App.DialogService.Show(sendViewModel.SelectFromViewModel);
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
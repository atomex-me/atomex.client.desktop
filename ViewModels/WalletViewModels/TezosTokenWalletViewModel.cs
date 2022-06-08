using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.Wallet.Tezos;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosTokenWalletViewModel : WalletViewModel
    {
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

            this.WhenAnyValue(vm => vm.TokenViewModel)
                .WhereNotNull()
                .SubscribeInMainThread(LoadTransfers);
        }

        private async void LoadTransfers(TezosTokenViewModel tokenViewModel)
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            if (tokenViewModel.IsFa12)
            {
                var tokenAccount = _app.Account.GetTezosTokenAccount<Fa12Account>(
                    currency: TezosTokenViewModel.Fa12,
                    tokenContract: tokenViewModel.Contract.Address,
                    tokenId: 0);

                Transactions = SortTransactions(
                    new ObservableCollection<TransactionViewModelBase>((await tokenAccount
                            .DataRepository
                            .GetTezosTokenTransfersAsync(tokenViewModel.Contract.Address,
                                offset: 0,
                                limit: int.MaxValue))
                        .Where(token => token.Token.TokenId == tokenViewModel.TokenBalance.TokenId)
                        .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                        .ToList()
                        .ForEachDo(t => t.OnClose = () => ShowRightPopupContent?.Invoke(null))));
            }
            else if (tokenViewModel.IsFa2)
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                Transactions = SortTransactions(
                    new ObservableCollection<TransactionViewModelBase>((await tezosAccount
                            .DataRepository
                            .GetTezosTokenTransfersAsync(tokenViewModel.Contract.Address,
                                offset: 0,
                                limit: int.MaxValue))
                        .Where(token => token.Token.TokenId == tokenViewModel.TokenBalance.TokenId)
                        .Select(t => new TezosTokenTransferViewModel(t, tezosConfig))
                        .ToList()
                        .ForEachDo(t => t.OnClose = () => ShowRightPopupContent?.Invoke(null))));
            }
        }

        protected override void SubscribeToServices()
        {
        }

        protected override void OnReceiveClick()
        {
            TokenViewModel?.ReceiveCommand.Execute().Subscribe();
        }

        protected override void OnSendClick()
        {
            TokenViewModel?.SendCommand.Execute().Subscribe();
        }

        protected override void OnConvertClick()
        {
            TokenViewModel?.ExchangeCommand.Execute().Subscribe();
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
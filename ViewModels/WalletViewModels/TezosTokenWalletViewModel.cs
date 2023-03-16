using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Desktop.Common;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Common;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class TezosTokenWalletViewModel : WalletViewModel
    {
        [Reactive] public TezosTokenViewModel? TokenViewModel { get; set; }

        public TezosTokenWalletViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public TezosTokenWalletViewModel(
            IAtomexApp app,
            Action<ViewModelBase?> showRightPopupContent) :
            base(app: app,
                currency: app.Account
                    .Currencies
                    .Get<TezosConfig>(TezosConfig.Xtz),
                showRightPopupContent: showRightPopupContent)
        {
            this.WhenAnyValue(vm => vm.TokenViewModel)
                .WhereNotNull()
                .Where(_ => this is not CollectibleWalletViewModel)
                .Select(tokenViewModel => tokenViewModel.TokenBalance.Name)
                .SubscribeInMainThread(header => Header = header);

            this.WhenAnyValue(vm => vm.TokenViewModel)
                .WhereNotNull()
                .SubscribeInMainThread(tokenViewModel =>
                {
                    LoadAddresses();
                    _ = LoadMoreTransactionsAsync(reset: true);
                });
            
            this.WhenAnyValue(vm => vm.TokenViewModel)
                .Where(token => token == null && AddressesViewModel != null)
                .SubscribeInMainThread(_ => AddressesViewModel.Dispose());
        }

        protected override bool FilterTransactions(TransactionsChangedEventArgs args, out IEnumerable<ITransaction>? txs)
        {
            if (Currencies.IsTezosTokenStandard(args.Currency))
            {
                txs = args.Transactions
                    .Where(t =>
                    {
                        if (t is not TezosTokenTransfer tokenTransfer)
                            return false;

                        return tokenTransfer?.Token?.Contract == TokenViewModel?.Contract?.Address &&
                               tokenTransfer?.Token?.TokenId == TokenViewModel?.TokenBalance?.TokenId;
                    })
                    .ToList();

                return true;
            }

            txs = null;
            return false;
        }

        protected override Task<List<TransactionInfo<ITransaction, ITransactionMetadata>>> LoadTransactionsWithMetadataAsync()
        {
            return Task.Run(async () =>
            {
                return (await _app
                    .LocalStorage
                    .GetTransactionsWithMetadataAsync(
                        currency: TokenViewModel.Contract.Type,
                        transactionType: typeof(TezosTokenTransfer),
                        metadataType: typeof(TransactionMetadata),
                        tokenContract: TokenViewModel.Contract.Address,
                        offset: _transactionsLoaded,
                        limit: TRANSACTIONS_LOADING_LIMIT,
                        sort: CurrentSortDirection != null ? CurrentSortDirection.Value : SortDirection.Desc))
                    .Where(t => t.Tx is TezosTokenTransfer transfer && transfer.Token.TokenId == TokenViewModel.TokenBalance.TokenId)
                    .ToList();
            });
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

        protected override void OnReachEndOfScroll()
        {
            _ = LoadMoreTransactionsAsync(reset: false);
        }

        protected override void LoadAddresses()
        {
            if (TokenViewModel == null)
                return;

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            AddressesViewModel?.Dispose();

            AddressesViewModel = new AddressesViewModel(
                app: _app,
                currency: tezosConfig,
                tokenType: TokenViewModel.Contract.Type,
                tokenContract: TokenViewModel.Contract.Address,
                tokenId: TokenViewModel.TokenBalance.TokenId);
        }

        protected override async Task OnUpdateClick()
        {
            _cancellation = new CancellationTokenSource();

            try
            {
                await Task.Run(async () =>
                {
                    var tezosAccount = _app.Account
                        .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                    var tezosTokensScanner = new TezosTokensWalletScanner(tezosAccount, TokenViewModel!.Contract.Type);

                    await tezosTokensScanner
                        .UpdateBalanceAsync(
                            tokenContract: TokenViewModel!.Contract.Address,
                            tokenId: (int)TokenViewModel!.TokenBalance.TokenId,
                            cancellationToken: _cancellation.Token)
                        .ConfigureAwait(false);
                });
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Tezos tokens Wallet update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos tokens Wallet update exception");
            }
        }

#if DEBUG
        protected override void DesignerMode()
        {
            base.DesignerMode();
        }
#endif
    }
}
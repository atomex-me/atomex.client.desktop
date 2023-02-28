using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Serilog;

using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.Core;
using Atomex.TezosTokens;
using Atomex.Wallet.Tezos;
using Atomex.Wallet;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class Fa2WalletViewModel : WalletViewModel
    {
        public new Fa2Config Currency => (Fa2Config)CurrencyViewModel.Currency;

        public Fa2WalletViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public Fa2WalletViewModel(
            IAtomexApp app,
            Action<CurrencyConfig> setConversionTab,
            Action<string>? setWertCurrency,
            Action<ViewModelBase?> showRightPopupContent,
            CurrencyConfig currency) : base(app, currency, showRightPopupContent, setConversionTab, setWertCurrency)
        {
        }

        protected override bool FilterTransactions(TransactionsChangedEventArgs args, out IEnumerable<ITransaction>? txs)
        {
            if (Currencies.IsTezosToken(args.Currency))
            {
                var tezosTokenConfig = (TezosTokenConfig)Currency;

                txs = args.Transactions
                    .Where(t =>
                    {
                        if (t is not TezosTokenTransfer tokenTransfer)
                            return false;

                        return tokenTransfer?.Token?.Contract == tezosTokenConfig.TokenContractAddress &&
                               tokenTransfer?.Token?.TokenId == tezosTokenConfig.TokenId;
                    })
                    .ToList();

                return true;
            }

            txs = null;
            return false;
        }

        protected override Task<List<(ITransaction Tx, ITransactionMetadata Metadata)>> LoadTransactionsWithMetadataAsync()
        {
            return Task.Run(async () =>
            {
                return (await _app.Account
                    .GetCurrencyAccount<Fa2Account>(Currency.Name)
                    .LocalStorage
                    .GetTokenTransfersWithMetadataAsync(
                        contractAddress: Currency.TokenContractAddress,
                        offset: _transactionsLoaded,
                        limit: TRANSACTIONS_LOADING_LIMIT,
                        sort: CurrentSortDirection != null ? CurrentSortDirection.Value : SortDirection.Desc)
                    .ConfigureAwait(false))
                    .Cast<(ITransaction, ITransactionMetadata)>()
                    .ToList();
            });
        }

        protected override void OnReceiveClick()
        {
            var tezosConfig = _app.Account.Currencies.GetByName(TezosConfig.Xtz);

            var receiveViewModel = new ReceiveViewModel(
                app: _app,
                currency: tezosConfig,
                tokenContract: Currency.TokenContractAddress,
                tokenType: "FA2");

            App.DialogService.Show(receiveViewModel);
        }

        protected override async Task OnUpdateClick()
        {
            _cancellation = new CancellationTokenSource();

            try
            {
                await Task.Run(async () =>
                {
                    await _app.Account
                        .GetCurrencyAccount<Fa2Account>(Currency.Name)
                        .UpdateBalanceAsync(_cancellation.Token)
                        .ConfigureAwait(false);
                });
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Wallet update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "Fa2WalletViewModel OnUpdate error");
                // todo: message to user!?
            }
        }

        protected override void LoadAddresses()
        {
            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            AddressesViewModel = new AddressesViewModel(
                app: _app,
                currency: tezosConfig,
                tokenContract: Currency.TokenContractAddress);
        }

#if DEBUG
        protected virtual void DesignerMode()
        {
        }
#endif
    }
}
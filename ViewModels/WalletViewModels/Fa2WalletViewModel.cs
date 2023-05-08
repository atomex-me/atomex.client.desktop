using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Avalonia.Controls;
using Serilog;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.TezosTokens;
using Atomex.Wallet.Tezos;
using Atomex.Wallet;
using Atomex.Wallets.Abstract;

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
            CurrencyConfig currency)
            : base(app, currency, showRightPopupContent, setConversionTab, setWertCurrency)
        {
        }

        protected override bool FilterTransactions(TransactionsChangedEventArgs args, out IEnumerable<ITransaction>? txs)
        {
            if (args.Currency == TezosHelper.Fa2)
            {
                txs = args.Transactions
                    .Cast<TezosTokenTransfer>()
                    .Where(t => t.Token?.Contract == Currency.TokenContractAddress &&
                                t.Token?.TokenId == Currency.TokenId)
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
                        currency: TezosHelper.Fa2,
                        transactionType: typeof(TezosTokenTransfer),
                        metadataType: typeof(TransactionMetadata),
                        tokenContract: Currency.TokenContractAddress,
                        offset: _transactionsLoaded,
                        limit: TRANSACTIONS_LOADING_LIMIT,
                        sort: CurrentSortDirection != null ? CurrentSortDirection.Value : SortDirection.Desc)
                    .ConfigureAwait(false))
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
                tokenType: TezosHelper.Fa2);

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
                        .UpdateBalanceAsync(App.LoggerFactory.CreateLogger<Fa2Account>(), _cancellation.Token)
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
                tokenType: TezosHelper.Fa2,
                tokenContract: Currency.TokenContractAddress,
                tokenId: Currency.TokenId);
        }

#if DEBUG
        protected virtual void DesignerMode()
        {
        }
#endif
    }
}
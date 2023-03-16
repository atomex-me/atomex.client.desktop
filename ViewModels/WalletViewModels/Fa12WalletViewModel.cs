using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Serilog;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.Core;
using Atomex.TezosTokens;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class Fa12WalletViewModel : WalletViewModel
    {
        public new Fa12Config Currency => (Fa12Config)CurrencyViewModel.Currency;

        public Fa12WalletViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public Fa12WalletViewModel(
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
            if (args.Currency == TezosHelper.Fa12)
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

        protected override async Task<List<TransactionInfo<ITransaction, ITransactionMetadata>>> LoadTransactionsWithMetadataAsync()
        {
            return await Task.Run(async () =>
            {
                return (await _app
                    .LocalStorage
                    .GetTransactionsWithMetadataAsync(
                        currency: TezosHelper.Fa12,
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
                tokenType: TezosHelper.Fa12);

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
                        .GetCurrencyAccount<Fa12Account>(Currency.Name)
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
                Log.Error(e, "Fa12WalletViewModel OnUpdate error");
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
                tokenType: TezosHelper.Fa12,
                tokenContract: Currency.TokenContractAddress);
        }

#if DEBUG
        protected virtual void DesignerMode()
        {
        }
#endif
    }
}
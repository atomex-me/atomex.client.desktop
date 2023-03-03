using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Ethereum;
using Atomex.Blockchain.Ethereum.Erc20;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.Wallet;
using Atomex.Wallet.Ethereum;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public class Erc20WalletViewModel : WalletViewModel
    {
        public new Erc20Config Currency => (Erc20Config)CurrencyViewModel.Currency;

        public Erc20WalletViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public Erc20WalletViewModel(
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
            if (args.Currency == EthereumHelper.Erc20)
            {
                txs = args.Transactions
                    .Cast<Erc20Transaction>()
                    .Where(t => t.Contract == Currency.TokenContractAddress)
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
                        currency: EthereumHelper.Erc20,
                        transactionType: typeof(Erc20Transaction),
                        metadataType: typeof(TransactionMetadata),
                        tokenContract: Currency.TokenContractAddress,
                        offset: _transactionsLoaded,
                        limit: TRANSACTIONS_LOADING_LIMIT,
                        sort: CurrentSortDirection != null ? CurrentSortDirection.Value : SortDirection.Desc)
                    .ConfigureAwait(false))
                    .ToList();
            });
        }
    }
}
using System;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Blockchain.Ethereum;
using Atomex.Blockchain.Tezos;
using Atomex.Core;
using Atomex.EthereumTokens;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public static class TransactionViewModelCreator
    {
        public static TransactionViewModel CreateViewModel(
            IBlockchainTransaction tx,
            CurrencyConfig currencyConfig)
        {
            return currencyConfig switch
            {
                BitcoinBasedConfig config =>
                    new BitcoinBasedTransactionViewModel(tx as BitcoinBasedTransaction, config),
                Erc20Config config =>
                    new EthereumERC20TransactionViewModel(tx as EthereumTransaction, config),
                EthereumConfig config =>
                    new EthereumTransactionViewModel(tx as EthereumTransaction, config),
                TezosConfig config =>
                    new TezosTransactionViewModel(tx as TezosTransaction, config),

                _ => throw new ArgumentOutOfRangeException("Not supported transaction type.")
            };
        }
    }
}
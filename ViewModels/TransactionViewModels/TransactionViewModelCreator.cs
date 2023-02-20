using System;
using System.Collections.Generic;
using System.Linq;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Bitcoin;
using Atomex.Blockchain.Ethereum;
using Atomex.Blockchain.Ethereum.Erc20;
using Atomex.Blockchain.Tezos;
using Atomex.Core;
using Atomex.EthereumTokens;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public static class TransactionViewModelCreator
    {
        public static IEnumerable<TransactionViewModel> CreateViewModels(
            ITransaction tx,
            ITransactionMetadata metadata,
            CurrencyConfig config)
        {
            if (config is BitcoinBasedConfig btcConfig &&
                tx is BitcoinTransaction btcTx &&
                metadata is TransactionMetadata btcMetadata)
            {
                return new List<TransactionViewModel>
                {
                    new BitcoinBasedTransactionViewModel(
                        tx: btcTx,
                        metadata: btcMetadata,
                        config: btcConfig)
                };
            }
            else if (config is Erc20Config erc20Config &&
                tx is Erc20Transaction erc20Tx &&
                metadata is TransactionMetadata erc20Metadata)
            {
                return erc20Tx.Transfers.Select((t, i) =>
                    new Erc20TransactionViewModel(
                        tx: erc20Tx,
                        metadata: erc20Metadata,
                        transferIndex: i,
                        config: erc20Config));
            }
            else if (config is EthereumConfig ethConfig &&
                tx is EthereumTransaction ethTx &&
                metadata is TransactionMetadata ethMetadata)
            {
                var txViewModel = new EthereumTransactionViewModel(
                    tx: ethTx,
                    metadata: ethMetadata,
                    config: ethConfig);

                if (ethTx.InternalTransactions != null && ethTx.InternalTransactions.Any())
                {
                    var internalsViewModels = ethTx.InternalTransactions
                        .Select((t, i) =>
                        {
                            return new EthereumTransactionViewModel(
                                tx: ethTx,
                                metadata: ethMetadata,
                                internalIndex: i,
                                config: ethConfig);
                        })
                        .ToList();

                    internalsViewModels.Add(txViewModel);

                    return internalsViewModels;
                }
            }
            else if (config is TezosConfig xtzConfig &&
                tx is TezosOperation xtzTx &&
                metadata is TransactionMetadata xtzMetadata)
            {
                return xtzTx.Operations.Select((t, i) => new TezosTransactionViewModel(
                    tx: xtzTx,
                    metadata: xtzMetadata,
                    internalIndex: i,
                    config: xtzConfig));
            }

            throw new ArgumentOutOfRangeException(nameof(config), "Not supported transaction type");
        }
    }
}
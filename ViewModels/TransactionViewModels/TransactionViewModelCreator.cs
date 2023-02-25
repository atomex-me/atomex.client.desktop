﻿using System;
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
                tx is BitcoinTransaction btcTx)
            {
                return new List<TransactionViewModel>
                {
                    new BitcoinBasedTransactionViewModel(
                        tx: btcTx,
                        metadata: metadata as TransactionMetadata,
                        config: btcConfig)
                };
            }
            else if (config is Erc20Config erc20Config &&
                     tx is Erc20Transaction erc20Tx)
            {
                return erc20Tx.Transfers
                    .Select((t, i) =>
                        new Erc20TransactionViewModel(
                            tx: erc20Tx,
                            metadata: metadata as TransactionMetadata,
                            transferIndex: i,
                            config: erc20Config))
                    .ToList();
            }
            else if (config is EthereumConfig ethConfig &&
                     tx is EthereumTransaction ethTx)
            {
                var txViewModel = new EthereumTransactionViewModel(
                    tx: ethTx,
                    metadata: metadata as TransactionMetadata,
                    config: ethConfig);

                if (ethTx.InternalTransactions != null && ethTx.InternalTransactions.Any())
                {
                    var internalsViewModels = ethTx.InternalTransactions
                        .Select((t, i) =>
                        {
                            return new EthereumTransactionViewModel(
                                tx: ethTx,
                                metadata: metadata as TransactionMetadata,
                                internalIndex: i,
                                config: ethConfig);
                        })
                        .ToList();

                    internalsViewModels.Add(txViewModel);

                    return internalsViewModels;
                }
            }
            else if (config is TezosConfig xtzConfig &&
                     tx is TezosOperation xtzTx)
            {
                return xtzTx.Operations
                    .Select((t, i) => new TezosTransactionViewModel(
                        tx: xtzTx,
                        metadata: metadata as TransactionMetadata,
                        internalIndex: i,
                        config: xtzConfig))
                    .ToList();
            }

            throw new ArgumentOutOfRangeException(nameof(config), "Not supported transaction type");
        }
    }
}
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
        public static List<TransactionViewModelBase> CreateViewModels(
            ITransaction tx,
            ITransactionMetadata? metadata,
            CurrencyConfig config)
        {
            if (Currencies.IsBitcoinBased(config.Name))
            {
                return new List<TransactionViewModelBase>
                {
                    new BitcoinBasedTransactionViewModel(
                        tx: (BitcoinTransaction)tx,
                        metadata: metadata as TransactionMetadata,
                        config: (BitcoinBasedConfig)config)
                };
            }
            else if (Currencies.IsEthereumToken(config.Name))
            {
                var erc20Tx = (Erc20Transaction)tx;

                return erc20Tx.Transfers
                    .Select((t, i) =>
                        new Erc20TransactionViewModel(
                            tx: erc20Tx,
                            metadata: metadata as TransactionMetadata,
                            transferIndex: i,
                            config: (Erc20Config)config))
                    .Cast<TransactionViewModelBase>()
                    .ToList();
            }
            else if (config.Name == EthereumHelper.Eth)
            {
                var ethTx = (EthereumTransaction)tx;

                var txViewModel = new EthereumTransactionViewModel(
                    tx: ethTx,
                    metadata: metadata as TransactionMetadata,
                    config: (EthereumConfig)config);

                if (ethTx.InternalTransactions == null || !ethTx.InternalTransactions.Any())
                    return new List<TransactionViewModelBase>
                    {
                        txViewModel
                    };

                var internalsViewModels = ethTx.InternalTransactions
                    .Select((t, i) =>
                    {
                        return new EthereumTransactionViewModel(
                            tx: ethTx,
                            metadata: metadata as TransactionMetadata,
                            internalIndex: i,
                            config: (EthereumConfig)config);
                    })
                    .Cast<TransactionViewModelBase>()
                    .ToList();

                internalsViewModels.Add(txViewModel);

                return internalsViewModels;
            }
            else if (Currencies.IsTezosBased(config.Name) && tx is TezosTokenTransfer tokenTranfer)
            {
                return new List<TransactionViewModelBase>
                {
                    new TezosTokenTransferViewModel(
                        tokenTranfer,
                        metadata as TransactionMetadata,
                        (TezosConfig)config)
                };
            }
            else if (config.Name == TezosHelper.Xtz)
            {
                var xtzTx = (TezosOperation)tx;

                return xtzTx.Operations
                    .Select((t, i) => new TezosTransactionViewModel(
                        tx: xtzTx,
                        metadata: metadata as TransactionMetadata,
                        internalIndex: i,
                        config: (TezosConfig)config))
                    .Cast<TransactionViewModelBase>()
                    .ToList();
            }

            throw new ArgumentOutOfRangeException(nameof(config), "Not supported transaction type");
        }
    }
}
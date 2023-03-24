using System;
using System.Linq;

using Avalonia.Controls;
using ReactiveUI.Fody.Helpers;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Tzkt.Operations;
using Atomex.Common;
using Atomex.Wallets.Abstract;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class TezosTransactionViewModel : TransactionViewModel
    {
        public string From { get; set; }
        public string To { get; set; }
        private decimal GasLimit { get; set; }
        private decimal GasUsed { get; set; }
        public string GasString => GasLimit == 0
            ? "0 / 0"
            : $"{GasUsed} / {GasLimit} ({GasUsed / GasLimit * 100:0.#}%)";
        private decimal StorageLimit { get; set; }
        private decimal StorageUsed { get; set; }
        public string StorageString => StorageLimit == 0
            ? "0 / 0"
            : $"{StorageUsed} / {StorageLimit} ({StorageUsed / StorageLimit * 100:0.#}%)";
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";
        [Reactive] public string Alias { get; set; }
        public int InternalIndex { get; set; }

        public TezosTransactionViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public TezosTransactionViewModel(
            TezosOperation tx,
            TransactionMetadata? metadata,
            int internalIndex,
            TezosConfig config)
            : base(
                tx: tx,
                metadata: metadata,
                config: config,
                amount: GetAmount(metadata, internalIndex),
                fee: GetFee(metadata, internalIndex),
                type: GetType(metadata, internalIndex))
        {
            InternalIndex = internalIndex;

            if (internalIndex < 0 || internalIndex >= tx.Operations.Count)
                throw new ArgumentOutOfRangeException(nameof(internalIndex));

            var operation = tx.Operations.ToList()[internalIndex];

            if (operation is TransactionOperation op)
            {
                From = op.Sender.Address;
                To = op.Target.Address;
                GasLimit = op.GasLimit;
                GasUsed = op.GasUsed;
                StorageLimit = op.StorageLimit;
                StorageUsed = op.StorageUsed;

                if (!string.IsNullOrEmpty(op.Target.Name))
                {
                    Alias = op.Target.Name;
                }
                else
                {
                    Alias = Amount <= 0
                        ? op.Target.Address.TruncateAddress()
                        : op.Sender.Address.TruncateAddress();
                }
            }
            else if (operation is DelegationOperation delegation)
            {
                From = delegation.Sender.Address;
                To = delegation.NewDelegate?.Address;
                GasLimit = delegation.GasLimit;
                GasUsed = delegation.GasUsed;
                StorageLimit = delegation.StorageLimit;
                
                if (delegation.NewDelegate?.Address != null)
                {
                    Direction = "to ";
                    Description = "Delegate";

                    if (!string.IsNullOrEmpty(delegation.NewDelegate?.Name))
                    {
                        Alias = delegation.NewDelegate?.Name;
                    }
                    else
                    {
                        Alias = delegation.NewDelegate.Address;
                    }
                }
                else
                {
                    Direction = "from ";
                    Description = "Undelegate";
                    Alias = From;
                }
            }
            else if (operation is RevealOperation reveal)
            {
                Direction = "from";
                From = reveal.Sender.Address;
                Description = "Reveal public key";
            }

            // others
        }

        public override void UpdateMetadata(ITransactionMetadata metadata, CurrencyConfig config)
        {
            TransactionMetadata = metadata;
            Amount = GetAmount((TransactionMetadata)metadata, InternalIndex);
            Fee = GetFee((TransactionMetadata)metadata, InternalIndex);
            Type = GetType((TransactionMetadata)metadata, InternalIndex);

            var tezosOperation = (TezosOperation)Transaction;
            var operation = tezosOperation.Operations.ToList()[InternalIndex];

            if (operation is TransactionOperation op)
            {
                Description = GetDescription(
                    type: Type,
                    amount: Amount,
                    decimals: config.Decimals,
                    currencyCode: config.Name);
                Direction = Amount <= 0 ? "to " : "from ";

                if (!string.IsNullOrEmpty(op.Target.Name))
                {
                    Alias = op.Target.Name;
                }
                else
                {
                    Alias = Amount <= 0
                        ? op.Target.Address.TruncateAddress()
                        : op.Sender.Address.TruncateAddress();
                }
            }
            else if (operation is DelegationOperation delegation)
            {
            }
            else if (operation is RevealOperation reveal)
            {
            }

            IsReady = metadata != null;
        }

        private static decimal GetAmount(
            TransactionMetadata? metadata,
            int internalIndex)
        {
            if (metadata == null)
                return 0;

            if (internalIndex < 0 || internalIndex >= metadata.Internals.Count)
                throw new ArgumentOutOfRangeException(nameof(internalIndex));

            return metadata.Internals[internalIndex].Amount.ToTez();
        }

        private static decimal GetFee(
            TransactionMetadata? metadata,
            int internalIndex)
        {
            if (metadata == null)
                return 0;

            if (internalIndex < 0 || internalIndex >= metadata.Internals.Count)
                throw new ArgumentOutOfRangeException(nameof(internalIndex));

            return metadata.Internals[internalIndex].Fee.ToTez();
        }

        private static TransactionType GetType(
            TransactionMetadata? metadata,
            int internalIndex)
        {
            if (metadata == null)
                return 0;

            if (internalIndex < 0 || internalIndex >= metadata.Internals.Count)
                throw new ArgumentOutOfRangeException(nameof(internalIndex));

            return metadata.Internals[internalIndex].Type;
        }

        private void DesignerMode()
        {
            //Id = "1234567890abcdefgh1234567890abcdefgh";
            From = "1234567890abcdefgh1234567890abcdefgh";
            To = "1234567890abcdefgh1234567890abcdefgh";
            //Time = DateTime.UtcNow;
            Description = "Exchange refund 0.030001 XTZ";
            CurrencyCode = TezosConfig.Xtz;
        }
    }
}
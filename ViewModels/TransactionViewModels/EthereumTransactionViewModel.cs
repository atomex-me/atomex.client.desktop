using System;

using Avalonia.Controls;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Ethereum;
using Atomex.Common;
using Atomex.Wallets.Abstract;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class EthereumTransactionViewModel : TransactionViewModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public decimal GasPrice { get; set; }
        private decimal GasLimit { get; set; }
        private decimal GasUsed { get; set; }
        public string GasString => GasLimit == 0
            ? "0 / 0"
            : $"{GasUsed} / {GasLimit} ({GasUsed / GasLimit * 100:0.#}%)";
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";
        public string Alias { get; set; }
        public int? InternalIndex { get; set; }

        public EthereumTransactionViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public EthereumTransactionViewModel(
            EthereumTransaction tx,
            TransactionMetadata? metadata,
            EthereumConfig config)
            : base(
                tx: tx,
                metadata: metadata,
                config: config,
                amount: GetAmount(metadata),
                fee: GetFee(metadata),
                type: metadata?.Type ?? TransactionType.Unknown)
        {
            From          = tx.From;
            To            = tx.To;
            GasPrice      = EthereumHelper.WeiToGwei(tx.GasPrice);
            GasLimit      = (decimal)tx.GasLimit;
            GasUsed       = (decimal)tx.GasUsed;
            InternalIndex = null;
            Alias         = Amount <= 0 ? To.TruncateAddress() : From.TruncateAddress();
        }

        public EthereumTransactionViewModel(
            EthereumTransaction tx,
            TransactionMetadata? metadata,
            int internalIndex,
            EthereumConfig config)
            : base(
                tx: tx,
                metadata: metadata,
                config: config,
                amount: GetInternalAmount(metadata, internalIndex),
                fee: GetInternalFee(metadata, internalIndex),
                type: GetInternalType(metadata, internalIndex))
        {
            InternalIndex = internalIndex;

            if (tx.InternalTransactions == null)
                throw new ArgumentException("InternalTransactions is null or empty", nameof(tx));

            if (internalIndex < 0 || internalIndex >= tx.InternalTransactions.Count)
                throw new ArgumentOutOfRangeException(nameof(internalIndex));

            From     = tx.InternalTransactions[internalIndex].From;
            To       = tx.InternalTransactions[internalIndex].To;
            GasPrice = EthereumHelper.WeiToGwei(tx.GasPrice);
            GasLimit = (decimal)tx.InternalTransactions[internalIndex].GasLimit;
            GasUsed  = (decimal)tx.InternalTransactions[internalIndex].GasUsed;
            Alias    = Amount <= 0 ? To.TruncateAddress() : From.TruncateAddress();
        }

        public override void UpdateMetadata(ITransactionMetadata metadata, CurrencyConfig config)
        {
            TransactionMetadata = metadata;

            if (InternalIndex == null)
            {
                Amount = GetAmount((TransactionMetadata)metadata);
                Fee    = GetFee((TransactionMetadata)metadata);
                Type   = metadata?.Type ?? TransactionType.Unknown;

            }
            else
            {
                Amount = GetInternalAmount((TransactionMetadata)metadata, InternalIndex.Value);
                Fee    = GetInternalFee((TransactionMetadata)metadata, InternalIndex.Value);
                Type   = GetInternalType((TransactionMetadata)metadata, InternalIndex.Value);
            }

            Alias = Amount <= 0 ? To.TruncateAddress() : From.TruncateAddress();
            Description = GetDescription(
                type: Type,
                amount: Amount,
                decimals: config.Decimals,
                currencyCode: config.Name);
            Direction = Amount <= 0 ? "to " : "from ";
            IsReady = metadata != null;
        }

        private static decimal GetAmount(TransactionMetadata? metadata)
        {
            return metadata?.Amount.WeiToEth() ?? 0m;
        }

        private static decimal GetInternalAmount(TransactionMetadata? metadata, int internalIndex)
        {
            if (metadata == null)
                return 0;

            if (internalIndex < 0 || internalIndex >= metadata.Internals.Count)
                throw new ArgumentOutOfRangeException(nameof(internalIndex));

            return metadata.Internals[internalIndex].Amount.WeiToEth();
        }

        private static decimal GetFee(TransactionMetadata? metadata)
        {
            return metadata != null
                ? EthereumHelper.WeiToEth(metadata.Fee)
                : 0;
        }

        private static decimal GetInternalFee(TransactionMetadata? metadata, int internalIndex)
        {
            if (metadata == null)
                return 0;

            if (internalIndex < 0 || internalIndex >= metadata.Internals.Count)
                throw new ArgumentOutOfRangeException(nameof(internalIndex));

            return EthereumHelper.WeiToEth(metadata.Internals[internalIndex].Fee);
        }

        private static TransactionType GetInternalType(TransactionMetadata? metadata, int internalIndex)
        {
            if (metadata == null)
                return 0;

            if (internalIndex < 0 || internalIndex >= metadata.Internals.Count)
                throw new ArgumentOutOfRangeException(nameof(internalIndex));

            return metadata.Internals[internalIndex].Type;
        }

        private void DesignerMode()
        {
            Transaction = new EthereumTransaction
            {
                Id = "0x1234567890abcdefgh1234567890abcdefgh",
                CreationTime = DateTime.UtcNow
            };
            From = "0x1234567890abcdefgh1234567890abcdefgh";
            To = "0x1234567890abcdefgh1234567890abcdefgh";
        }
    }
}
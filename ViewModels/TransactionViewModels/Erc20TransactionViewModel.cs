using System;

using Avalonia.Controls;
using ReactiveUI.Fody.Helpers;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Ethereum.Erc20;
using Atomex.Common;
using Atomex.Core;
using Atomex.EthereumTokens;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class Erc20TransactionViewModel : TransactionViewModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";
        [Reactive] public string Alias { get; set; }
        public int TransferIndex { get; set; }

        public Erc20TransactionViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public Erc20TransactionViewModel(
            Erc20Transaction tx,
            TransactionMetadata? metadata,
            int transferIndex,
            Erc20Config config)
            : base(
                tx: tx, 
                metadata: metadata,
                config: config,
                amount: GetAmount(metadata, transferIndex, config),
                fee: 0m,
                type: GetType(metadata, transferIndex))
        {
            TransferIndex = transferIndex;
            From = tx.Transfers[transferIndex].From;
            To = tx.Transfers[transferIndex].To;
            Alias = Amount <= 0 ? To.TruncateAddress() : From.TruncateAddress();
        }

        public override void UpdateMetadata(ITransactionMetadata metadata, CurrencyConfig config)
        {
            TransactionMetadata = metadata;
            Amount = GetAmount((TransactionMetadata)metadata, TransferIndex, (Erc20Config)config);
            Type = GetType((TransactionMetadata)metadata, TransferIndex);
            Description = GetDescription(
                type: Type,
                amount: Amount,
                fee: Fee,
                decimals: config.Decimals,
                currencyCode: config.Name);
            Direction = Amount <= 0 ? "to " : "from ";
            Alias = Amount <= 0 ? To.TruncateAddress() : From.TruncateAddress();

            IsReady = metadata != null;
        }

        private static decimal GetAmount(
            TransactionMetadata? metadata,
            int transferIndex,
            Erc20Config config)
        {
            if (metadata == null)
                return 0;

            if (transferIndex < 0 || transferIndex >= metadata.Internals.Count)
                throw new ArgumentOutOfRangeException(nameof(transferIndex));

            return metadata.Internals[transferIndex].Amount.FromTokens(config.Decimals);
        }

        private static TransactionType GetType(
            TransactionMetadata? metadata,
            int transferIndex)
        {
            if (metadata == null)
                return TransactionType.Unknown;

            if (transferIndex < 0 || transferIndex >= metadata.Internals.Count)
                throw new ArgumentOutOfRangeException(nameof(transferIndex));

            return metadata.Internals[transferIndex].Type;
        }

        protected override void DesignerMode()
        {
            Transaction = new Erc20Transaction()
            {
                Id = "0x1234567890abcdefgh1234567890abcdefgh"
            };
            From = "0x1234567890abcdefgh1234567890abcdefgh";
            To = "0x1234567890abcdefgh1234567890abcdefgh";
        }
    }
}
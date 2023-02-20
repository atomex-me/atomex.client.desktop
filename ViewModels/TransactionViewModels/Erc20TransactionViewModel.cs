using System;

using Avalonia.Controls;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Ethereum.Erc20;
using Atomex.Common;
using Atomex.EthereumTokens;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class Erc20TransactionViewModel : TransactionViewModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";
        public string Alias { get; set; }

        public Erc20TransactionViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public Erc20TransactionViewModel(
            Erc20Transaction tx,
            TransactionMetadata metadata,
            int transferIndex,
            Erc20Config config)
            : base(tx: tx, 
                metadata: metadata,
                config: config,
                amount: GetAmount(metadata, transferIndex, config),
                fee: 0m,
                type: GetType(metadata, transferIndex))
        {
            From = tx.Transfers[transferIndex].From;
            To = tx.Transfers[transferIndex].To;

            Alias = Amount switch
            {
                <= 0 => tx.Transfers[transferIndex].To.TruncateAddress(),
                > 0 => tx.Transfers[transferIndex].From.TruncateAddress()
            };
        }

        private static decimal GetAmount(
            TransactionMetadata metadata,
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
            TransactionMetadata metadata,
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
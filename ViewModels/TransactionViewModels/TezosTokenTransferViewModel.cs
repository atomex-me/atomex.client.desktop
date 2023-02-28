using System;

using Avalonia.Controls;
using ReactiveUI.Fody.Helpers;

using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class TezosTokenTransferViewModel : TransactionViewModelBase
    {
        private const int MaxAmountDecimals = CurrencyConfig.MaxPrecision;
        public string From { get; set; }
        public string To { get; set; }
        public string CurrencyCode { get; set; }
        public string TxHash => Id.Split("/")[0];
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";      
        [Reactive] public string Alias { get; set; }
        [Reactive] public string Direction { get; set; }

        public TezosTokenTransferViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public TezosTokenTransferViewModel(
            TezosTokenTransfer tx,
            TransactionMetadata? metadata,
            TezosConfig config)
        {
            Currency = config ?? throw new ArgumentNullException(nameof(config));
            Transaction = tx ?? throw new ArgumentNullException(nameof(tx));
            TransactionMetadata = metadata;
            From = tx.From;
            To = tx.To;
            Amount = metadata != null ? metadata.Amount.FromTokens(tx.Token.Decimals) : 0;
            AmountFormat = $"F{Math.Min(tx.Token.Decimals, MaxAmountDecimals)}";
            CurrencyCode = tx.Token.Symbol;
            Type = metadata?.Type ?? TransactionType.Unknown;

            Description = TransactionViewModel.GetDescription(
                type: Type,
                amount: Amount,
                decimals: tx.Token.Decimals,
                currencyCode: tx.Token.Symbol);

            Alias = tx.GetAlias(Type);
            Direction = Amount <= 0 ? "to " : "from ";
        }

        public override void UpdateMetadata(ITransactionMetadata metadata, CurrencyConfig config)
        {
            var tx = (TezosTokenTransfer)Transaction;

            TransactionMetadata = metadata;
            Amount = metadata != null ? metadata.Amount.FromTokens(tx.Token.Decimals) : 0;
            Type = metadata?.Type ?? TransactionType.Unknown;

            Description = TransactionViewModel.GetDescription(
                type: Type,
                amount: Amount,
                decimals: tx.Token.Decimals,
                currencyCode: tx.Token.Symbol);

            Alias = tx.GetAlias(Type);
            Direction = Amount <= 0 ? "to " : "from ";
            IsReady = metadata != null;
        }

        private void DesignerMode()
        {
            Transaction = new TezosTokenTransfer
            {
                Id = "1234567890abcdefgh1234567890abcdefgh",
                CreationTime = DateTime.UtcNow
            };
            From = "1234567890abcdefgh1234567890abcdefgh";
            To = "1234567890abcdefgh1234567890abcdefgh";
        }
    }
}
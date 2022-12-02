using System;

using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.ViewModels;
using Avalonia.Controls;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class TezosTokenTransferViewModel : TransactionViewModelBase
    {
        private const int MaxAmountDecimals = AddressesHelper.MaxTokenCurrencyFormatDecimals;
        public string From { get; set; }
        public string To { get; set; }
        public string CurrencyCode { get; set; }
        public string TxHash => Id.Split("/")[0];
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";
        
        public string Alias { get; set; }
        public string Direction { get; set; }

        public TezosTokenTransferViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public TezosTokenTransferViewModel(TokenTransfer tx, TezosConfig tezosConfig)
        {
            Currency = tezosConfig ?? throw new ArgumentNullException(nameof(tezosConfig));

            Transaction  = tx ?? throw new ArgumentNullException(nameof(tx));
            Id           = tx.Id;
            State        = tx.Status;
            Type         = tx.Type;
            From         = tx.From;
            To           = tx.To;
            Amount       = GetAmount(tx);
            AmountFormat = $"F{Math.Min(tx.Token.Decimals, MaxAmountDecimals)}";
            CurrencyCode = tx.Token.Symbol;
            Time         = tx.CreationTime ?? DateTime.UtcNow;

            Description = TransactionViewModel.GetDescription(
                type: tx.Type,
                amount: Amount,
                netAmount: Amount,
                amountDigits: tx.Token.Decimals,
                currencyCode: tx.Token.Symbol);

            Alias = tx.GetAlias();
            Direction = Amount <= 0 ? "to ": "from ";
        }

        private static decimal GetAmount(TokenTransfer tx)
        {
            if (tx.Amount.TryParseWithRound(tx.Token.Decimals, out var amount))
            {
                var sign = tx.Type.HasFlag(TransactionType.Input)
                    ? 1
                    : -1;

                return sign * amount;
            }

            return 0;
        }

        private void DesignerMode()
        {
            Id   = "1234567890abcdefgh1234567890abcdefgh";
            From = "1234567890abcdefgh1234567890abcdefgh";
            To   = "1234567890abcdefgh1234567890abcdefgh";
            Time = DateTime.UtcNow;
        }
    }
}
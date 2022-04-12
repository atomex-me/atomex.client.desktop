using System;
using System.Windows.Input;

using ReactiveUI;
using Serilog;

using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.Client.Desktop.Common;
using Atomex.Core;
using Atomex.ViewModels;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class TezosTokenTransferViewModel : TransactionViewModelBase
    {
        public const int MaxAmountDecimals = AddressesHelper.MaxTokenCurrencyFormatDecimals;
        
        public string From { get; set; }
        public string To { get; set; }
        public string CurrencyCode { get; set; }

        public string TxExplorerUri => $"{Currency.TxExplorerUri}{Id}";
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";
        
        public string Alias { get; set; }
        public string Direction { get; set; }

        public TezosTokenTransferViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public TezosTokenTransferViewModel(TokenTransfer tx, TezosConfig tezosConfig)
        {
            Currency = tezosConfig ?? throw new ArgumentNullException(nameof(tezosConfig));

            Transaction  = tx ?? throw new ArgumentNullException(nameof(tx));
            State        = Transaction.State;
            Type         = Transaction.Type;
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

        private ICommand _openTxInExplorerCommand;
        public ICommand OpenTxInExplorerCommand => _openTxInExplorerCommand ??= ReactiveCommand.Create<string>((id) =>
        {
            if (Uri.TryCreate($"{Currency.TxExplorerUri}{id}", UriKind.Absolute, out var uri))
                App.OpenBrowser(uri.ToString());
            else
                Log.Error("Invalid uri for transaction explorer");
        });

        private ICommand _openAddressInExplorerCommand;
        public ICommand OpenAddressInExplorerCommand => _openAddressInExplorerCommand ??= ReactiveCommand.Create<string>((address) =>
        {
            if (Uri.TryCreate($"{Currency.AddressExplorerUri}{address}", UriKind.Absolute, out var uri))
                App.OpenBrowser(uri.ToString());
            else
                Log.Error("Invalid uri for address explorer");
        });

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ??= ReactiveCommand.Create<string>((s) =>
        {
            try
            {
                App.Clipboard.SetTextAsync(s);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });

        private static decimal GetAmount(TokenTransfer tx)
        {
            if (tx.Amount.TryParseWithRound(tx.Token.Decimals, out var amount))
            {
                var sign = tx.Type.HasFlag(BlockchainTransactionType.Input)
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
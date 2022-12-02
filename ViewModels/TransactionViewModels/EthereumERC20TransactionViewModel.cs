using System;

using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Ethereum;
using Atomex.EthereumTokens;
using Atomex.ViewModels;
using Avalonia.Controls;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class EthereumErc20TransactionViewModel : TransactionViewModel
    {
        public string From { get; set; }    
        public string To { get; set; }
        public decimal GasPrice { get; set; }
        public decimal GasUsed { get; set; }
        public bool IsInternal { get; set; }
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";
        public string Alias { get; set; }

        public EthereumErc20TransactionViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public EthereumErc20TransactionViewModel(
            EthereumTransaction tx,
            Erc20Config erc20Config)
            : base(tx, erc20Config, GetAmount(tx, erc20Config), 0)
        {
            From       = tx.From;
            To         = tx.To;
            GasPrice   = EthereumConfig.WeiToGwei((decimal)tx.GasPrice);
            GasUsed    = (decimal)tx.GasUsed;
            Fee        = EthereumConfig.WeiToEth(tx.GasUsed * tx.GasPrice);
            IsInternal = tx.IsInternal;

            Alias = Amount switch
            {
                <= 0 => tx.To.TruncateAddress(),
                > 0 => tx.From.TruncateAddress()
            };
        }

        public static decimal GetAmount(
            EthereumTransaction tx,
            Erc20Config erc20Config)
        {
            var result = 0m;
            
            if (tx.Type.HasFlag(TransactionType.SwapRedeem) ||
                tx.Type.HasFlag(TransactionType.SwapRefund))
                result += erc20Config.TokenDigitsToTokens(tx.Amount);
            else
            {
                if (tx.Type.HasFlag(TransactionType.Input))
                    result += erc20Config.TokenDigitsToTokens(tx.Amount);
                if (tx.Type.HasFlag(TransactionType.Output))
                    result += -erc20Config.TokenDigitsToTokens(tx.Amount);
            }

            tx.InternalTxs?.ForEach(t => result += GetAmount(t, erc20Config));

            return result;
        }

        private void DesignerMode()
        {
            Id   = "0x1234567890abcdefgh1234567890abcdefgh";
            From = "0x1234567890abcdefgh1234567890abcdefgh";
            To   = "0x1234567890abcdefgh1234567890abcdefgh";
            Time = DateTime.UtcNow;
        }
    }
}
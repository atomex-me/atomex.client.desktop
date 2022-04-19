using System;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.ViewModels;
using Avalonia.Controls;

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
        public string Alias { get; set; }

        public TezosTransactionViewModel()
        {
#if DEBUG
            if (Design.IsDesignMode)
                DesignerMode();
#endif
        }

        public TezosTransactionViewModel(TezosTransaction tx, TezosConfig tezosConfig)
            : base(tx, tezosConfig, GetAmount(tx, tezosConfig), GetFee(tx))
        {
            From = tx.From;
            To = tx.To;
            GasLimit = tx.GasLimit;
            GasUsed = tx.GasUsed;
            StorageLimit = tx.StorageLimit;
            StorageUsed = tx.StorageUsed;
            Fee = TezosConfig.MtzToTz(tx.Fee);

            if (!string.IsNullOrEmpty(tx.Alias))
            {
                Alias = tx.Alias;
            }
            else
            {
                Alias = Amount switch
                {
                    <= 0 => tx.To.TruncateAddress(),
                    > 0 => tx.From.TruncateAddress()
                };
            }
        }

        private static decimal GetAmount(TezosTransaction tx, TezosConfig tezosConfig)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.Input))
                result += tx.Amount / tezosConfig.DigitsMultiplier;

            var includeFee = tezosConfig.Name == tezosConfig.FeeCurrencyName;
            var fee = includeFee ? tx.Fee : 0;

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += -(tx.Amount + fee) / tezosConfig.DigitsMultiplier;

            tx.InternalTxs?.ForEach(t => result += GetAmount(t, tezosConfig));

            return result;
        }

        private static decimal GetFee(TezosTransaction tx)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += TezosConfig.MtzToTz(tx.Fee);

            tx.InternalTxs?.ForEach(t => result += GetFee(t));

            return result;
        }

        private void DesignerMode()
        {
            Id = "1234567890abcdefgh1234567890abcdefgh";
            From = "1234567890abcdefgh1234567890abcdefgh";
            To = "1234567890abcdefgh1234567890abcdefgh";
            Time = DateTime.UtcNow;
            Description = "Exchange refund 0.030001 XTZ";
            CurrencyCode = TezosConfig.Xtz;
        }
    }
}
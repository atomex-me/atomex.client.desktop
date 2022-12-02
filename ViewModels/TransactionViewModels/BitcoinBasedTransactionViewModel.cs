using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Bitcoin;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class BitcoinBasedTransactionViewModel : TransactionViewModel
    {
        public BitcoinBasedTransactionViewModel(
            BitcoinTransaction tx,
            BitcoinBasedConfig bitcoinBasedConfig)
            : base(tx, bitcoinBasedConfig, tx.Amount / bitcoinBasedConfig.DigitsMultiplier,
                GetFee(tx, bitcoinBasedConfig))
        {
            Fee = tx.Fees != null
                ? tx.Fees.Value / bitcoinBasedConfig.DigitsMultiplier
                : 0; // todo: N/A
        }

        private static decimal GetFee(
            BitcoinTransaction tx,
            BitcoinBasedConfig bitcoinBasedConfig)
        {
            return tx.Fees != null
                ? tx.Type.HasFlag(TransactionType.Output)
                    ? tx.Fees.Value / bitcoinBasedConfig.DigitsMultiplier
                    : 0
                : 0;
        }
    }
}
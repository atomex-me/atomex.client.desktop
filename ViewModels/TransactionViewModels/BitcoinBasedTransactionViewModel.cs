using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Bitcoin;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class BitcoinBasedTransactionViewModel : TransactionViewModel
    {
        public BitcoinBasedTransactionViewModel(
            BitcoinTransaction tx,
            TransactionMetadata? metadata,
            BitcoinBasedConfig config)
            : base(tx: tx,
                  metadata: metadata,
                  config: config,
                  amount: GetAmount(metadata, config),
                  fee: GetFee(metadata, config),
                  type: metadata?.Type ?? TransactionType.Unknown)
        {
        }

        private static decimal GetAmount(
            TransactionMetadata? metadata,
            BitcoinBasedConfig config)
        {
            return metadata != null ? config.SatoshiToCoin(metadata.Amount) : 0;
        }

        private static decimal GetFee(
            TransactionMetadata? metadata,
            BitcoinBasedConfig config)
        {
            return metadata != null ? config.SatoshiToCoin(metadata.Fee) : 0;
        }
    }
}
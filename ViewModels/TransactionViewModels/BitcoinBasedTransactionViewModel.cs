using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Bitcoin;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.TransactionViewModels
{
    public class BitcoinBasedTransactionViewModel : TransactionViewModel
    {
        public BitcoinBasedTransactionViewModel(
            BitcoinTransaction tx,
            TransactionMetadata? metadata,
            BitcoinBasedConfig config)
            : base(
                tx: tx,
                metadata: metadata,
                config: config,
                amount: GetAmount(metadata, config),
                fee: GetFee(metadata, config),
                type: GetType(metadata))
        {
        }

        public override void UpdateMetadata(ITransactionMetadata metadata, CurrencyConfig config)
        {
            TransactionMetadata = metadata;
            Amount = GetAmount((TransactionMetadata)metadata, (BitcoinBasedConfig)config);
            Fee = GetFee((TransactionMetadata)metadata, (BitcoinBasedConfig)config);
            Type = GetType((TransactionMetadata)metadata);
            Description = GetDescription(
                type: Type,
                amount: Amount,
                fee: Fee,
                decimals: config.Decimals,
                currencyCode: config.Name);
            Direction = Amount <= 0 ? "to " : "from ";

            IsReady = metadata != null;
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

        private static TransactionType GetType(
            TransactionMetadata? metadata)
        {
            return metadata?.Type ?? TransactionType.Unknown;
        }
    }
}
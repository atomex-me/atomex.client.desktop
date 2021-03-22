using System;
using Atomex.Core;
using Atomex.Client.Desktop.ViewModels.Abstract;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class CurrencyViewModelCreator
    {
        public static CurrencyViewModel CreateViewModel(Currency currency)
        {
            return CreateViewModel(currency, subscribeToUpdates: true);
        }

        public static CurrencyViewModel CreateViewModel(
            Currency currency,
            bool subscribeToUpdates)
        {
            var result = currency.Name switch
            {
                "BTC" => (CurrencyViewModel) new BitcoinCurrencyViewModel(currency),
                "LTC" => new LitecoinCurrencyViewModel(currency),
                "USDT" => new TetherCurrencyViewModel(currency),
                "TBTC" => new TbtcCurrencyViewModel(currency),
                "WBTC" => new WbtcCurrencyViewModel(currency),
                "ETH" => new EthereumCurrencyViewModel(currency),
                "NYX" => new NYXCurrencyViewModel(currency),
                "FA2" => new FA2CurrencyViewModel(currency),
                "TZBTC" => new TzbtcCurrencyViewModel(currency),
                "KUSD" => new KusdCurrencyViewModel(currency),
                "XTZ" => new TezosCurrencyViewModel(currency),
                "FA12" => new TzbtcCurrencyViewModel(currency),
                _ => throw new NotSupportedException(
                    $"Can't create currency view model for {currency.Name}. This currency is not supported.")
            };

            if (!subscribeToUpdates)
                return result;

            result.SubscribeToUpdates(App.AtomexApp.Account);
            result.SubscribeToRatesProvider(App.AtomexApp.QuotesProvider);
            result.UpdateInBackgroundAsync();

            return result;
        }
    }
}
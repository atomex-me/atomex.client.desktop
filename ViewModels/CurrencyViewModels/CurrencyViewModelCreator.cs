using System;
using System.Collections.Concurrent;
using Atomex.Wallets.Abstract;

// ReSharper disable InconsistentNaming

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public enum Currencies
    {
        BTC,
        LTC,
        USDT,
        TBTC,
        WBTC,
        ETH,
        TZBTC,
        KUSD,
        USDT_XTZ,
        XTZ,
    }

    public static class CurrencyViewModelCreator
    {
        private static readonly ConcurrentDictionary<Currencies, CurrencyViewModel> Instances = new();

        public static CurrencyViewModel CreateOrGet(CurrencyConfig currency)
        {
            return CreateOrGet(currency, subscribeToUpdates: true);
        }

        public static CurrencyViewModel CreateOrGet(CurrencyConfig currencyConfig, bool subscribeToUpdates)
        {
            var parsed = Enum.TryParse(currencyConfig.Name, out Currencies currency);

            if (!parsed)
                throw NotSupported(currencyConfig.Name);

            if (subscribeToUpdates && Instances.TryGetValue(currency, out var cachedCurrencyViewModel))
                return cachedCurrencyViewModel;

            var currencyViewModel = currency switch
            {
                Currencies.BTC      => (CurrencyViewModel)new BitcoinCurrencyViewModel(currencyConfig),
                Currencies.LTC      => new LitecoinCurrencyViewModel(currencyConfig),
                Currencies.USDT     => new TetherCurrencyViewModel(currencyConfig),
                Currencies.TBTC     => new TbtcCurrencyViewModel(currencyConfig),
                Currencies.WBTC     => new WbtcCurrencyViewModel(currencyConfig),
                Currencies.ETH      => new EthereumCurrencyViewModel(currencyConfig),
                Currencies.TZBTC    => new TzbtcCurrencyViewModel(currencyConfig),
                Currencies.KUSD     => new KusdCurrencyViewModel(currencyConfig),
                Currencies.USDT_XTZ => new UsdtXtzCurrencyViewModel(currencyConfig),
                Currencies.XTZ      => new TezosCurrencyViewModel(currencyConfig),
                _ => throw NotSupported(currencyConfig.Name)
            };

            if (!subscribeToUpdates) return currencyViewModel;

            currencyViewModel.SubscribeToUpdates(App.AtomexApp.Account, App.AtomexApp.LocalStorage);
            currencyViewModel.SubscribeToRatesProvider(App.AtomexApp.QuotesProvider);
            currencyViewModel.UpdateInBackgroundAsync();
            Instances.TryAdd(currency, currencyViewModel);

            return currencyViewModel;
        }

        public static void Reset()
        {
            foreach (var currencyViewModel in Instances.Values)
            {
                currencyViewModel.Dispose();
            }

            Instances.Clear();
        }

        private static NotSupportedException NotSupported(string currencyName)
        {
            return new NotSupportedException(
                $"Can't create currency view model for {currencyName}. This currency is not supported.");
        }
    }
}
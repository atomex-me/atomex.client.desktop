using System;
using System.Linq;
using System.Reactive;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Atomex.Abstract;
using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.Common.Configuration;
using Atomex.Core;
using ReactiveUI;
using Atomex.Wallets.Abstract;

namespace Atomex.Client.Desktop.Common
{
    public static class DesignTime
    {
        private static Assembly CoreAssembly { get; } = AppDomain.CurrentDomain
            .GetAssemblies()
            .First(a => a.GetName().Name == "Atomex.Client.Core");

        private static readonly IConfiguration CurrenciesConfiguration = new ConfigurationBuilder()
            .AddEmbeddedJsonFile(CoreAssembly, "currencies.json")
            .Build();

        private static readonly IConfiguration SymbolsConfiguration = new ConfigurationBuilder()
            .AddEmbeddedJsonFile(CoreAssembly, "symbols.json")
            .Build();

        public static readonly ICurrencies TestNetCurrencies
            = new Currencies(CurrenciesConfiguration.GetSection(Network.TestNet.ToString()));

        public static readonly ICurrencies MainNetCurrencies
            = new Currencies(CurrenciesConfiguration.GetSection(Network.MainNet.ToString()));

        public static readonly ISymbols TestNetSymbols
            = new Symbols(SymbolsConfiguration.GetSection(Network.TestNet.ToString()));

        public static CurrencyViewModel BtcCurrencyViewModel { get; } = GetCurrencyViewModel(
            TestNetCurrencies.Get<BitcoinConfig>("BTC"));


        private static CurrencyViewModel GetCurrencyViewModel(CurrencyConfig config)
        {
            var currencyViewModel = CurrencyViewModelCreator.CreateOrGet(
                config, subscribeToUpdates: false);

            currencyViewModel.TotalAmount = 123.32m;
            currencyViewModel.TotalAmountInBase = 34500.56m;

            return currencyViewModel;
        }

        public static readonly ReactiveCommand<Unit, Unit> EmptyCommand = ReactiveCommand.Create(() => { });
    }
}
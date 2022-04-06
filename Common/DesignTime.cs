using System;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.Configuration;

using Atomex.Abstract;
using Atomex.Common.Configuration;
using Atomex.Core;

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
    }
}
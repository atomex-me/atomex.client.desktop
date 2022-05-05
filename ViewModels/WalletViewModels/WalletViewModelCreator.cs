using System;

using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public static class WalletViewModelCreator
    {
        public static IWalletViewModel CreateViewModel(
            IAtomexApp app,
            Action<CurrencyConfig_OLD> setConversionTab,
            CurrencyConfig_OLD currency)
        {
            return currency switch
            {
                BitcoinBasedConfig_OLD _ or
                    Erc20Config _ or
                    EthereumConfig_ETH _ => new WalletViewModel(
                        app: app,
                        setConversionTab: setConversionTab,
                        currency: currency),

                Fa12Config _ => new Fa12WalletViewModel(
                    app: app,
                    setConversionTab: setConversionTab,
                    currency: currency),

                TezosConfig_OLD _ => new TezosWalletViewModel(
                    app: app,
                    setConversionTab: setConversionTab,
                    currency: currency),

                _ => throw new NotSupportedException($"Can't create wallet view model for {currency.Name}. This currency is not supported."),
            };
        }
    }
}
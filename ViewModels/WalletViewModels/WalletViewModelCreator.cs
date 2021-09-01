using System;
using Atomex.Client.Desktop.Controls;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public static class WalletViewModelCreator
    {
        public static WalletViewModel CreateViewModel(
            IAtomexApp app,
            Action<CurrencyConfig> setConversionTab,
            CurrencyConfig currency)
        {
            switch (currency)
            {
                case BitcoinBasedConfig _:
                case Erc20Config _:
                case EthereumConfig _:
                case Fa2Config _:
                case Fa12Config _:
                    return new WalletViewModel(
                        app: app,
                        setConversionTab: setConversionTab,
                        currency: currency);
                case TezosConfig _:
                    return new TezosWalletViewModel(
                        app: app,
                        setConversionTab: setConversionTab,
                        currency: currency);
                default:
                    throw new NotSupportedException(
                        $"Can't create wallet view model for {currency.Name}. This currency is not supported.");
            }
        }
    }
}
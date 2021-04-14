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
            Action<Currency> setConversionTab,
            Currency currency)
        {
            switch (currency)
            {
                case BitcoinBasedCurrency _:
                case ERC20 _:
                case Ethereum _:
                case NYX _:
                case FA2 _:
                case FA12 _:
                    return new WalletViewModel(
                        app: app,
                        setConversionTab: setConversionTab,
                        currency: currency);
                case Tezos _:
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
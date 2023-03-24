using System;
using System.Collections.Generic;

using Atomex.Client.Desktop.ViewModels.CurrencyViewModels;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;
using Atomex.Wallets.Abstract;

namespace Atomex.Client.Desktop.ViewModels.WalletViewModels
{
    public static class WalletViewModelCreator
    {
        public static IWalletViewModel CreateViewModel(
            IAtomexApp app,
            Action<CurrencyConfig> setConversionTab,
            Action<string>? setWertCurrency,
            Action<ViewModelBase?> showRightPopupContent,
            Action<TezosTokenViewModel> showTezosToken,
            Action<IEnumerable<TezosTokenViewModel>> showTezosCollection,
            CurrencyConfig currency)
        {
            return currency switch
            {
                BitcoinBasedConfig _ => new WalletViewModel(
                        app: app,
                        setConversionTab: setConversionTab,
                        setWertCurrency: setWertCurrency,
                        showRightPopupContent: showRightPopupContent,
                        currency: currency),

                Erc20Config _ => new Erc20WalletViewModel(
                    app: app,
                    setConversionTab: setConversionTab,
                    setWertCurrency: setWertCurrency,
                    showRightPopupContent: showRightPopupContent,
                    currency: currency),

                EthereumConfig _ => new WalletViewModel(
                    app: app,
                    setConversionTab: setConversionTab,
                    setWertCurrency: setWertCurrency,
                    showRightPopupContent: showRightPopupContent,
                    currency: currency),

                Fa12Config _ => new Fa12WalletViewModel(
                    app: app,
                    setConversionTab: setConversionTab,
                    setWertCurrency: setWertCurrency,
                    showRightPopupContent: showRightPopupContent,
                    currency: currency),

                Fa2Config _ => new Fa2WalletViewModel(
                    app: app,
                    setConversionTab: setConversionTab,
                    setWertCurrency: setWertCurrency,
                    showRightPopupContent: showRightPopupContent,
                    currency: currency),

                TezosConfig _ => new TezosWalletViewModel(
                    app: app,
                    setConversionTab: setConversionTab,
                    setWertCurrency: setWertCurrency,
                    showRightPopupContent: showRightPopupContent,
                    showTezosToken: showTezosToken,
                    showTezosCollection: showTezosCollection,
                    currency: currency),

                _ => throw new NotSupportedException($"Can't create wallet view model for {currency.Name}. This currency is not supported"),
            };
        }
    }
}
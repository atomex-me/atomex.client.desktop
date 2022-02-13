using System;

using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public static class SendViewModelCreator
    {
        public static SendViewModel CreateViewModel(IAtomexApp app, CurrencyConfig currency)
        {
            return currency switch
            {
                BitcoinBasedConfig _ => new BitcoinBasedSendViewModel(app, currency),
                Erc20Config _        => new Erc20SendViewModel(app, currency),
                EthereumConfig _     => new EthereumSendViewModel(app, currency),
                Fa12Config _         => new Fa12SendViewModel(app, currency),
                TezosConfig _        => new TezosSendViewModel(app, currency),
                _ => throw new NotSupportedException($"Can't create send view model for {currency.Name}. This currency is not supported."),
            };
        }
    }
}
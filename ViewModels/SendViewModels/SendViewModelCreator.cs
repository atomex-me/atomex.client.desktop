using System;
using Atomex.Client.Desktop.Controls;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace Atomex.Client.Desktop.ViewModels.SendViewModels
{
    public static class SendViewModelCreator
    {
        public static SendViewModel CreateViewModel(
            IAtomexApp app,
            Currency currency)
        {
            return currency switch
            {
                BitcoinBasedCurrency _ => (SendViewModel) new BitcoinBasedSendViewModel(app, currency),
                ERC20 _ => (SendViewModel)new Erc20SendViewModel(app, currency),
                Ethereum _ => (SendViewModel)new EthereumSendViewModel(app, currency),
                NYX _ => (SendViewModel)new NYXSendViewModel(app, currency),
                FA2 _ => (SendViewModel)new FA2SendViewModel(app, currency),
                FA12 _ => (SendViewModel)new Fa12SendViewModel(app, currency),
                Tezos _ => (SendViewModel)new TezosSendViewModel(app, currency),
                _ => throw new NotSupportedException($"Can't create send view model for {currency.Name}. This currency is not supported."),
            };
        }

        public static int GetSendPageId(Currency currency)
        {
            return 0;
            // return currency switch
            // {
            //     BitcoinBasedCurrency _ => Pages.SendBitcoinBased,
            //     ERC20 _ => Pages.SendErc20,
            //     Ethereum _ => Pages.SendEthereum,
            //     NYX _ => Pages.SendNYX,
            //     FA2 _ => Pages.SendFA2,
            //     FA12 _ => Pages.SendFa12,
            //     Tezos _ => Pages.SendTezos,
            //     _ => throw new NotSupportedException($"Can't get send page id for currency {currency.Name}. This currency is not supported."),
            // };
        }
    }
}
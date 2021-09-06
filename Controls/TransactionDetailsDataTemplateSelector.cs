using System;
using Atomex.Blockchain.Abstract;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Avalonia.Markup.Xaml.Templates;

namespace Atomex.Client.Desktop.Controls
{
    public class TransactionDetailsDataTemplateSelector : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public IControl Build(object data)
        {
            return GetTemplate(data)?.Build(data) ?? new TextBlock {Text = "Transaction Template Not Found"};
        }

        private static DataTemplate? GetTemplate(object data)
        {
            if (!(data is TransactionViewModel transaction))
                return null;

            switch (transaction.Currency.Name)
            {
                case "BTC":
                    return App.TemplateService.GetTxDetailsTemplate(
                        TxDetailsTemplate.BitcoinBasedTransactionDetailsTemplate);
                case "LTC":
                    return App.TemplateService.GetTxDetailsTemplate(
                        TxDetailsTemplate.BitcoinBasedTransactionDetailsTemplate);
                case "ETH":
                    return App.TemplateService.GetTxDetailsTemplate(
                        TxDetailsTemplate.EthereumTransactionDetailsTemplate);
                case "XTZ":
                    return App.TemplateService.GetTxDetailsTemplate(
                        TxDetailsTemplate.TezosTransactionDetailsTemplate);
                case "USDT":
                    return App.TemplateService.GetTxDetailsTemplate(
                        TxDetailsTemplate.EthereumERC20TransactionDetailsTemplate);
                case "TBTC":
                    return App.TemplateService.GetTxDetailsTemplate(
                        TxDetailsTemplate.EthereumERC20TransactionDetailsTemplate);
                case "WBTC":
                    return App.TemplateService.GetTxDetailsTemplate(
                        TxDetailsTemplate.EthereumERC20TransactionDetailsTemplate);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool Match(object data)
        {
            return data is TransactionViewModel;
        }
    }
}
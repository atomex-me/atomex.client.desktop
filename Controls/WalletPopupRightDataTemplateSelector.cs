using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;

using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Atomex.EthereumTokens;

namespace Atomex.Client.Desktop.Controls
{
    public class WalletPopupRightDataTemplateSelector : IDataTemplate
    {
        public bool SupportsRecycling => false;
        private readonly ViewLocator _viewLocator = new ViewLocator();

        public IControl Build(object data)
        {
            return GetTemplate(data)?.Build(data) ?? _viewLocator.Build(data);
        }

        private static DataTemplate? GetTemplate(object data)
        {
            if (data is not TransactionViewModelBase transaction)
                return null;

            if (transaction is TezosTokenTransferViewModel)
                return App.TemplateService.GetTxDetailsTemplate(TxDetailsTemplate.TezosTokenTransferDetailsTemplate);

            return transaction.Currency switch
            {
                BitcoinBasedConfig =>
                    App.TemplateService.GetTxDetailsTemplate(TxDetailsTemplate.BitcoinBasedTransactionDetailsTemplate),
                Erc20Config =>
                    App.TemplateService.GetTxDetailsTemplate(TxDetailsTemplate.Erc20TransactionDetailsTemplate),
                EthereumConfig =>
                    App.TemplateService.GetTxDetailsTemplate(TxDetailsTemplate.EthereumTransactionDetailsTemplate),
                TezosConfig =>
                    App.TemplateService.GetTxDetailsTemplate(TxDetailsTemplate.TezosTransactionDetailsTemplate),
                _ => null
            };
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }
    }
}
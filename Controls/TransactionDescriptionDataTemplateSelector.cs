using System;
using Atomex.Blockchain.Abstract;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Avalonia.Markup.Xaml.Templates;

namespace Atomex.Client.Desktop.Controls
{
    public class TransactionDescriptionDataTemplateSelector : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public IControl Build(object data)
        {
            return GetTemplate(data)?.Build(data) ?? new TextBlock {Text = "Transaction Template Not Found"};
        }

        private static DataTemplate? GetTemplate(object data)
        {
            if (data is TezosTokenTransferViewModel)
                return App.TemplateService.GetTxDescriptionTemplate(TxDescriptionTemplate.XtzAdditionalDescriptionTemplate);

            if (data is not TransactionViewModel tx)
                return null;

            return tx.Currency switch
            {
                BitcoinBasedConfig => App.TemplateService.GetTxDescriptionTemplate(TxDescriptionTemplate
                    .BtcBasedDescriptionTemplate),
                TezosConfig => App.TemplateService.GetTxDescriptionTemplate(TxDescriptionTemplate
                    .XtzAdditionalDescriptionTemplate),
                EthereumConfig => App.TemplateService.GetTxDescriptionTemplate(TxDescriptionTemplate
                    .EthAdditionalDescriptionTemplate),
                _ => App.TemplateService.GetTxDescriptionTemplate(TxDescriptionTemplate.BtcBasedDescriptionTemplate)
            };
        }

        public bool Match(object data)
        {
            return data is ITransactionViewModel;
        }
    }
}
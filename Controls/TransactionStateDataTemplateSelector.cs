using System;
using Atomex.Blockchain.Abstract;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Avalonia.Markup.Xaml.Templates;

namespace Atomex.Client.Desktop.Controls
{
    public class TransactionStateDataTemplateSelector : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public IControl Build(object data)
        {
            return GetTemplate(data)?.Build(data) ?? new TextBlock {Text = "Transaction Template Not Found"};
        }

        private static DataTemplate? GetTemplate(object data)
        {
            if (data is not ITransactionViewModel transaction)
                return null;

            return transaction.State switch
            {
                BlockchainTransactionState.Unknown => App.TemplateService.GetTxStateTemplate(TxStateTemplate
                    .UnknownStateTemplate),
                BlockchainTransactionState.Pending => App.TemplateService.GetTxStateTemplate(TxStateTemplate
                    .PendingStateTemplate),
                BlockchainTransactionState.Confirmed => App.TemplateService.GetTxStateTemplate(TxStateTemplate
                    .ConfirmedStateTemplate),
                BlockchainTransactionState.Unconfirmed => App.TemplateService.GetTxStateTemplate(TxStateTemplate
                    .UnconfirmedStateTemplate),
                BlockchainTransactionState.Failed => App.TemplateService.GetTxStateTemplate(TxStateTemplate
                    .FailedStateTemplate),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public bool Match(object data)
        {
            return data is TransactionViewModel;
        }
    }
}
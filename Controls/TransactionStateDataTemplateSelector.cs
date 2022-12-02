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
            if (data is not TransactionViewModelBase transaction)
                return null;

            return transaction.State switch
            {
                TransactionStatus.Pending => App.TemplateService.GetTxStateTemplate(TxStateTemplate
                    .PendingStateTemplate),
                TransactionStatus.Confirmed => App.TemplateService.GetTxStateTemplate(TxStateTemplate
                    .ConfirmedStateTemplate),
                TransactionStatus.Unconfirmed => App.TemplateService.GetTxStateTemplate(TxStateTemplate
                    .UnconfirmedStateTemplate),
                TransactionStatus.Failed => App.TemplateService.GetTxStateTemplate(TxStateTemplate
                    .FailedStateTemplate),
                TransactionStatus.Unknown => App.TemplateService.GetTxStateTemplate(TxStateTemplate
                    .UnknownStateTemplate),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public bool Match(object data)
        {
            return data is TransactionViewModelBase;
        }
    }
}
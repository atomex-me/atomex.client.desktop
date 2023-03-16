using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;

using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;

namespace Atomex.Client.Desktop.Controls
{
    public class TransactionTypeDataTemplateSelector : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public IControl Build(object data)
        {
            return GetTemplate(data)?.Build(data) ?? new TextBlock {Text = "Transaction Template Not Found"};
        }

        private static DataTemplate? GetTemplate(object data)
        {
            if (data is not TransactionViewModelBase tx)
                return null;

            if (tx.Type.HasFlag(TransactionType.SwapPayment))
                return App.TemplateService.GetTxTypeTemplate(TxTypeTemplate.SwapPaymentTypeTemplate);

            if (tx.Type.HasFlag(TransactionType.SwapRefund))
                return App.TemplateService.GetTxTypeTemplate(TxTypeTemplate.SwapRefundTypeTemplate);

            if (tx.Type.HasFlag(TransactionType.SwapRedeem))
                return App.TemplateService.GetTxTypeTemplate(TxTypeTemplate.SwapRedeemTypeTemplate);

            if (tx.Type.HasFlag(TransactionType.TokenApprove))
                return App.TemplateService.GetTxTypeTemplate(TxTypeTemplate.TokenApproveTypeTemplate);

            if (tx.Type.HasFlag(TransactionType.ContractCall))
                return App.TemplateService.GetTxTypeTemplate(TxTypeTemplate.TokenApproveTypeTemplate);

            if (tx.Amount <= 0)
                return App.TemplateService.GetTxTypeTemplate(TxTypeTemplate.SentTypeTemplate);

            if (tx.Amount > 0)
                return App.TemplateService.GetTxTypeTemplate(TxTypeTemplate.ReceivedTypeTemplate);

            return App.TemplateService.GetTxTypeTemplate(TxTypeTemplate.UnknownTypeTemplate);
        }

        public bool Match(object data)
        {
            return data is TransactionViewModelBase;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;

namespace Atomex.Client.Desktop.Services
{
    public enum TxTypeTemplate
    {
        SwapPaymentTypeTemplate,
        SwapRefundTypeTemplate,
        SwapRedeemTypeTemplate,
        TokenApproveTypeTemplate,
        SentTypeTemplate,
        ReceivedTypeTemplate,
        UnknownTypeTemplate
    }
    
    public enum TxStateTemplate {
        PendingStateTemplate,
        ConfirmedStateTemplate,
        UnconfirmedStateTemplate,
        FailedStateTemplate,
        UnknownStateTemplate
    }

    public enum TxDetailsTemplate
    {
        TransactionDetailsTemplate,
        BitcoinBasedTransactionDetailsTemplate,
        TezosTransactionDetailsTemplate,
        TezosTokenTransferDetailsTemplate,
        EthereumTransactionDetailsTemplate,
        EthereumERC20TransactionDetailsTemplate
    }

    public enum TxDescriptionTemplate
    {
        BtcBasedDescriptionTemplate,
        XtzAdditionalDescriptionTemplate,
        EthAdditionalDescriptionTemplate,
    }

    public enum SwapStateTemplate
    {
        SwapCanceledTemplate,
        SwapInProgressTemplate,
        SwapCompletedTemplate,
        SwapRefundTemplate,
        SwapUnsettledTemplate
    }
    
    public enum BeaconOperationTemplate
    {
        BeaconTransactionTemplate,
        BeaconRevealTemplate,
    }

    public class TemplateService
    {
        public IDictionary<string, DataTemplate> Templates;
        

        public TemplateService()
        {
            Templates = new Dictionary<string, DataTemplate>();
            
            LoadTemplates(typeof(TxTypeTemplate));
            LoadTemplates(typeof(TxStateTemplate));
            LoadTemplates(typeof(TxDetailsTemplate));
            LoadTemplates(typeof(SwapStateTemplate));
            LoadTemplates(typeof(TxDescriptionTemplate));
            LoadTemplates(typeof(BeaconOperationTemplate));
        }
        
        public DataTemplate GetTxTypeTemplate(TxTypeTemplate templateType)
        {
            return Templates.TryGetValue(templateType.ToString(), out var template)
                ? template
                : Templates[TxTypeTemplate.UnknownTypeTemplate.ToString()];
        }

        public DataTemplate GetTxStateTemplate(TxStateTemplate templateType)
        {
            return Templates.TryGetValue(templateType.ToString(), out var template)
                ? template
                : Templates[TxStateTemplate.UnknownStateTemplate.ToString()];
        }

        public DataTemplate GetTxDetailsTemplate(TxDetailsTemplate templateType)
        {
            return Templates.TryGetValue(templateType.ToString(), out var template)
                ? template
                : Templates[TxDetailsTemplate.TransactionDetailsTemplate.ToString()];
        }
        
        public DataTemplate GetSwapStateTemplate(SwapStateTemplate templateType)
        {
            return Templates.TryGetValue(templateType.ToString(), out var template)
                ? template
                : Templates[SwapStateTemplate.SwapUnsettledTemplate.ToString()];
        }
        
        public DataTemplate GetTxDescriptionTemplate(TxDescriptionTemplate templateType)
        {
            return Templates.TryGetValue(templateType.ToString(), out var template)
                ? template
                : Templates[TxDescriptionTemplate.BtcBasedDescriptionTemplate.ToString()];
        }
        
        public DataTemplate GetBeaconOperationTemplate(BeaconOperationTemplate templateType)
        {
            return Templates.TryGetValue(templateType.ToString(), out var template)
                ? template
                : Templates[BeaconOperationTemplate.BeaconTransactionTemplate.ToString()];
        }

        private void LoadTemplates(Type enumType)
        {
            var templates = new List<string>(Enum.GetNames(enumType));
            templates
                .ForEach(templateName =>
                {
                    Templates.Add(templateName, (DataTemplate) App.Current.FindResource(templateName));
                });
        }
    }
}
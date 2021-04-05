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
        UnknownStateTemplate,
        PendingStateTemplate,
        ConfirmedStateTemplate,
        UnconfirmedStateTemplate,
        FailedStateTemplate
    }

    public class TemplateService
    {
        public IDictionary<string, DataTemplate> Templates;

        public TemplateService()
        {
            Templates = new Dictionary<string, DataTemplate>();

            LoadTxTypeTemplates();
            LoadTxStateTemplates();
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

        private void LoadTxTypeTemplates()
        {
            var txTypeTemplates = new List<string>(Enum.GetNames(typeof(TxTypeTemplate)));
            txTypeTemplates
                .ForEach(templateName =>
                {
                    Templates.Add(templateName, (DataTemplate) App.Current.FindResource(templateName));
                });
        }

        private void LoadTxStateTemplates()
        {
            var txstateTemplates = new List<string>(Enum.GetNames(typeof(TxStateTemplate)));
            txstateTemplates
                .ForEach(templateName =>
                {
                    Templates.Add(templateName, (DataTemplate) App.Current.FindResource(templateName));
                });
        }
    }
}
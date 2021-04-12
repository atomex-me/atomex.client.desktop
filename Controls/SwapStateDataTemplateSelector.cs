using System;
using Atomex.Blockchain.Abstract;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels;
using Atomex.Client.Desktop.ViewModels.TransactionViewModels;
using Avalonia.Markup.Xaml.Templates;

namespace Atomex.Client.Desktop.Controls
{
    public class SwapStateDataTemplateSelector : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public IControl Build(object data)
        {
            return GetTemplate(data)?.Build(data) ?? new TextBlock {Text = "Transaction Template Not Found"};
        }

        private static DataTemplate? GetTemplate(object data)
        {
            if (!(data is SwapViewModel swap))
                return null;

            switch (swap.CompactState)
            {
                case SwapCompactState.Canceled:
                    return App.TemplateService.GetSwapStateTemplate(SwapStateTemplate.SwapCanceledTemplate);
                case SwapCompactState.InProgress:
                    return App.TemplateService.GetSwapStateTemplate(SwapStateTemplate.SwapInProgressTemplate);
                case SwapCompactState.Completed:
                    return App.TemplateService.GetSwapStateTemplate(SwapStateTemplate.SwapCompletedTemplate);
                case SwapCompactState.Refunded:
                    return App.TemplateService.GetSwapStateTemplate(SwapStateTemplate.SwapRefundTemplate);
                case SwapCompactState.Unsettled:
                    return App.TemplateService.GetSwapStateTemplate(SwapStateTemplate.SwapUnsettledTemplate);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool Match(object data)
        {
            return data is SwapViewModel;
        }
    }
}
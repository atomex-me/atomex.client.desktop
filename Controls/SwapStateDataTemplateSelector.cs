using System;

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;

using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels;

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
            if (data is not SwapViewModel swap)
                return null;

            return swap.CompactState switch
            {
                SwapCompactState.Canceled   => App.TemplateService.GetSwapStateTemplate(SwapStateTemplate.SwapCanceledTemplate),
                SwapCompactState.InProgress => App.TemplateService.GetSwapStateTemplate(SwapStateTemplate.SwapInProgressTemplate),
                SwapCompactState.Completed  => App.TemplateService.GetSwapStateTemplate(SwapStateTemplate.SwapCompletedTemplate),
                SwapCompactState.Refunded   => App.TemplateService.GetSwapStateTemplate(SwapStateTemplate.SwapRefundTemplate),
                SwapCompactState.Unsettled  => App.TemplateService.GetSwapStateTemplate(SwapStateTemplate.SwapUnsettledTemplate),
                _ => throw new ArgumentOutOfRangeException($"Unknown swap compact state {swap.CompactState}"),
            };
        }

        public bool Match(object data) => data is SwapViewModel;
    }
}
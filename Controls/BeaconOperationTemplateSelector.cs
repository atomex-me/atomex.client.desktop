using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Atomex.Client.Desktop.Services;
using Atomex.Client.Desktop.ViewModels.DappsViewModels;
using Avalonia.Markup.Xaml.Templates;

namespace Atomex.Client.Desktop.Controls
{
    public class BeaconOperationTemplateSelector : IDataTemplate
    {
        public bool SupportsRecycling => false;
        private readonly ViewLocator _viewLocator = new();

        public IControl Build(object data)
        {
            return GetTemplate(data)?.Build(data) ?? _viewLocator.Build(data);
        }

        private static DataTemplate? GetTemplate(object data)
        {
            if (data is not BaseBeaconOperationViewModel operation)
                return null;

            return operation switch
            {
                TransactionContentViewModel => App.TemplateService
                    .GetBeaconOperationTemplate(BeaconOperationTemplate.BeaconTransactionTemplate),
                
                RevealContentViewModel => App.TemplateService
                    .GetBeaconOperationTemplate(BeaconOperationTemplate.BeaconRevealTemplate),
                
                _ => null
            };
        }

        public bool Match(object data)
        {
            return data is BaseBeaconOperationViewModel;
        }
    }
}
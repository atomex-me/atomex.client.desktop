using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.Abstract
{
    public interface IConversionViewModel
    {
        CurrencyConfig FromCurrency { get; set; }
        CurrencyConfig ToCurrency { get; set; }
    }
}
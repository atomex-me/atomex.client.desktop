using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.Abstract
{
    public interface IConversionViewModel
    {
        Currency FromCurrency { get; set; }
        Currency ToCurrency { get; set; }
    }
}
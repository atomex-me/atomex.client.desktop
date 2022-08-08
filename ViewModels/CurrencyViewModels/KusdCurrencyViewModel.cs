using Avalonia.Media;

using Atomex.Client.Desktop.Properties;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class KusdCurrencyViewModel : CurrencyViewModel
    {
        public KusdCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header           = Currency.Description;
            AccentColor      = Color.FromRgb(r: 7, g: 82, b: 192);
            IconPath         = $"{PathToIcons}/kusd.svg";
            DisabledIconPath = $"{PathToIcons}/kusd-disabled.svg";
            FeeName          = Resources.SvMiningFee;
        }
    }
}

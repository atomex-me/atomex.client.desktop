using Avalonia.Media;

using Atomex.Client.Desktop.Properties;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class LitecoinCurrencyViewModel : CurrencyViewModel
    {
        public LitecoinCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header           = Currency.Description;
            AccentColor      = Color.FromRgb(r: 191, g: 191, b: 191);
            IconPath         = $"{PathToIcons}/litecoin.svg";
            DisabledIconPath = $"{PathToIcons}/litecoin-disabled.svg";
            FeeName          = Resources.SvMiningFee;
        }
    }
}
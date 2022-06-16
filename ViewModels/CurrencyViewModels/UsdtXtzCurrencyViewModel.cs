using Avalonia.Media;

using Atomex.Client.Desktop.Properties;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class UsdtXtzCurrencyViewModel : CurrencyViewModel
    {
        public UsdtXtzCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header              = Currency.Description;
            AccentColor         = Color.FromRgb(r: 7, g: 82, b: 192);
            IconPath            = $"{PathToIcons}/tether-tezos.svg";
            DisabledIconPath    = $"{PathToIcons}/tether-tezos-disabled.svg";
            FeeName             = Resources.SvMiningFee;
        }
    }
}

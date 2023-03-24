using Avalonia.Media;

using Atomex.Client.Desktop.Properties;
using Atomex.Wallets.Abstract;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TzbtcCurrencyViewModel : CurrencyViewModel
    {
        public TzbtcCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header           = Currency.Description;
            AccentColor      = Color.FromRgb(r: 7, g: 82, b: 192);
            IconPath         = $"{PathToIcons}/tzbtc.svg";
            DisabledIconPath = $"{PathToIcons}/tzbtc-disabled.svg";
            FeeName          = Resources.SvMiningFee;
        }
    }
}
using Avalonia.Media;

using Atomex.Client.Desktop.Properties;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TezosCurrencyViewModel : CurrencyViewModel
    {
        public TezosCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header           = Currency.Description;
            AccentColor      = Color.FromRgb(r: 44, g: 125, b: 247);
            IconPath         = $"{PathToIcons}/tezos.svg";
            DisabledIconPath = $"{PathToIcons}/tezos-disabled.svg";
            FeeName          = Resources.SvMiningFee;
        }
    }
}
using Avalonia.Media;

using Atomex.Client.Desktop.Properties;
using Atomex.Wallets.Abstract;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class BitcoinCurrencyViewModel : CurrencyViewModel
    {
        public BitcoinCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header           = Currency.Description;
            AccentColor      = Color.FromRgb(r: 255, g: 148, b: 0);
            IconPath         = $"{PathToIcons}/bitcoin.svg";
            DisabledIconPath = $"{PathToIcons}/bitcoin-disabled.svg";
            FeeName          = Resources.SvMiningFee;
        }
    }
}
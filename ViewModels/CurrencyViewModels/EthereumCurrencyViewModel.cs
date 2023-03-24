using Avalonia.Media;

using Atomex.Client.Desktop.Properties;
using Atomex.Wallets.Abstract;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class EthereumCurrencyViewModel : CurrencyViewModel
    {
        public EthereumCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header           = Currency.Description;
            AccentColor      = Color.FromRgb(r: 73, g: 114, b: 143);
            IconPath         = $"{PathToIcons}/ethereum.svg";
            DisabledIconPath = $"{PathToIcons}/ethereum-disabled.svg";
            FeeName          = Resources.SvGasLimit;
        }
    }
}
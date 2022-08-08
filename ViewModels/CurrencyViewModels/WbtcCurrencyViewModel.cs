using Avalonia.Media;

using Atomex.Client.Desktop.Properties;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class WbtcCurrencyViewModel : CurrencyViewModel
    {
        public decimal AvailableAmountInChainCurrency { get; set; }

        public WbtcCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            ChainCurrency    = new EthereumConfig();
            Header           = Currency.Description;
            AccentColor      = Color.FromRgb(r: 7, g: 82, b: 192);
            IconPath         = $"{PathToIcons}/wbtc.svg";
            DisabledIconPath = $"{PathToIcons}/wbtc-disabled.svg";
            FeeName          = Resources.SvGasLimit;
        }
    }
}
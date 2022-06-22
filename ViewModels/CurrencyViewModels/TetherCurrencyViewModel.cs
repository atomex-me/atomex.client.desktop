using Avalonia.Media;

using Atomex.Client.Desktop.Properties;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TetherCurrencyViewModel : CurrencyViewModel
    {
        public decimal AvailableAmountInChainCurrency { get; set; }

        public TetherCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            ChainCurrency    = new EthereumConfig();
            Header           = Currency.Description;
            AccentColor      = Color.FromRgb(r: 0, g: 162, b: 122);
            IconPath         = $"{PathToIcons}/tether.svg";
            DisabledIconPath = $"{PathToIcons}/tether-disabled.svg";
            FeeName          = Resources.SvGasLimit;
        }
    }
}
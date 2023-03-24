using Avalonia.Media;

using Atomex.Client.Desktop.Properties;
using Atomex.Wallets.Abstract;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TbtcCurrencyViewModel : CurrencyViewModel
    {
        public decimal AvailableAmountInChainCurrency { get; set; }

        public TbtcCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            ChainCurrency    = new EthereumConfig();
            Header           = Currency.Description;
            AccentColor      = Color.FromRgb(r: 7, g: 82, b: 192);
            IconPath         = $"{PathToIcons}/tbtc.svg";
            DisabledIconPath = $"{PathToIcons}/tbtc-disabled.svg";
            FeeName          = Resources.SvGasLimit;
        }
    }
}
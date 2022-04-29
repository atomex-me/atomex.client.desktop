using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Avalonia.Visuals.Media.Imaging;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class WbtcCurrencyViewModel : CurrencyViewModel
    {
        public decimal AvailableAmountInChainCurrency { get; set; }

        public WbtcCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            var iconBrush = new ImageBrush(GetBitmap(PathToImage("wbtc_90x90.png")));
            var iconMaskBrush = new ImageBrush(GetBitmap(PathToImage("wbtc_mask.png")));
            iconBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            iconMaskBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            
            ChainCurrency       = new EthereumConfig();
            Header              = Currency.Description;
            IconBrush           = iconBrush;
            IconMaskBrush       = iconMaskBrush;
            AccentColor         = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = $"{PathToIcons}/wbtc.svg";
            DisabledIconPath    = $"{PathToIcons}/wbtc-disabled.svg";
            FeeName             = Resources.SvGasLimit;
        }
    }
}
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Avalonia.Visuals.Media.Imaging;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TetherCurrencyViewModel : CurrencyViewModel
    {
        public decimal AvailableAmountInChainCurrency { get; set; }

        public TetherCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            var iconBrush = new ImageBrush(GetBitmap(PathToImage("tether_90x90.png")));
            var iconMaskBrush = new ImageBrush(GetBitmap(PathToImage("tether_mask.png")));
            iconBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            iconMaskBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            
            ChainCurrency       = new EthereumConfig();
            Header              = Currency.Description;
            IconBrush           = iconBrush;
            IconMaskBrush       = iconMaskBrush;
            AccentColor         = Color.FromRgb(r: 0, g: 162, b: 122);
            AmountColor         = Color.FromRgb(r: 183, g: 208, b: 225);
            UnselectedIconBrush = Brushes.White;
            IconPath            = $"{PathToIcons}/tether.svg";
            DisabledIconPath    = $"{PathToIcons}/tether-disabled.svg";
            FeeName             = Resources.SvGasLimit;
        }
    }
}
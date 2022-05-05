using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Avalonia.Visuals.Media.Imaging;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TbtcCurrencyViewModel : CurrencyViewModel
    {
        public decimal AvailableAmountInChainCurrency { get; set; }

        public TbtcCurrencyViewModel(CurrencyConfig_OLD currency)
            : base(currency)
        {
            var iconBrush = new ImageBrush(GetBitmap(PathToImage("tbtc_90x90_dark.png")));
            var iconMaskBrush = new ImageBrush(GetBitmap(PathToImage("tbtc_mask.png")));
            iconBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            iconMaskBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            
            ChainCurrency       = new EthereumConfig_ETH();
            Header              = Currency.Description;
            IconBrush           = iconBrush;
            IconMaskBrush       = iconMaskBrush;
            AccentColor         = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = GetBitmap(PathToImage("tbtc_dark.png"));
            LargeIconPath       = GetBitmap(PathToImage("tbtc_90x90_dark.png"));
            FeeName             = Resources.SvGasLimit;
        }
    }
}
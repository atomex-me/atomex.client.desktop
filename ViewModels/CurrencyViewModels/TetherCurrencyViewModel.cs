using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Avalonia.Visuals.Media.Imaging;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TetherCurrencyViewModel : CurrencyViewModel
    {
        public decimal AvailableAmountInChainCurrency { get; set; }

        public TetherCurrencyViewModel(CurrencyConfig_OLD currency)
            : base(currency)
        {
            var iconBrush = new ImageBrush(GetBitmap(PathToImage("tether_90x90.png")));
            var iconMaskBrush = new ImageBrush(GetBitmap(PathToImage("tether_mask.png")));
            iconBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            iconMaskBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            
            ChainCurrency       = new EthereumConfig_ETH();
            Header              = Currency.Description;
            IconBrush           = iconBrush;
            IconMaskBrush       = iconMaskBrush;
            AccentColor         = Color.FromRgb(r: 0, g: 162, b: 122);
            AmountColor         = Color.FromRgb(r: 183, g: 208, b: 225);
            UnselectedIconBrush = Brushes.White;
            IconPath            = GetBitmap(PathToImage("tether.png"));
            LargeIconPath       = GetBitmap(PathToImage("tether_90x90.png"));
            FeeName             = Resources.SvGasLimit;
        }
    }
}
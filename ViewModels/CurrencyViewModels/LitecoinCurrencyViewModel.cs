using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Avalonia.Visuals.Media.Imaging;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class LitecoinCurrencyViewModel : CurrencyViewModel
    {
        public LitecoinCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            var iconBrush = new ImageBrush(GetBitmap(PathToImage("litecoin_90x90.png")));
            var iconMaskBrush = new ImageBrush(GetBitmap(PathToImage("litecoin_mask.png")));
            iconBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            iconMaskBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            
            Header              = Currency.Description;
            IconBrush           = iconBrush;
            IconMaskBrush       = iconMaskBrush;
            AccentColor         = Color.FromRgb(r: 191, g: 191, b: 191);
            AmountColor         = Color.FromRgb(r: 231, g: 231, b: 231);
            UnselectedIconBrush = Brushes.White;
            IconPath            = $"{PathToIcons}/litecoin.svg";
            DisabledIconPath    = $"{PathToIcons}/litecoin-disabled.svg";
            FeeName             = Resources.SvMiningFee;
        }
    }
}
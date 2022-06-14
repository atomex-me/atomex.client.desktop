using Avalonia.Media;
using Avalonia.Visuals.Media.Imaging;

using Atomex.Client.Desktop.Properties;
using Atomex.Core;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class UsdtXtzCurrencyViewModel : CurrencyViewModel
    {
        public UsdtXtzCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            var iconBrush = new ImageBrush(GetBitmap(PathToImage("kusd_90x90.png")));
            var iconMaskBrush = new ImageBrush(GetBitmap(PathToImage("kusd_mask.png")));
            iconBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            iconMaskBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;

            Header              = Currency.Description;
            IconBrush           = iconBrush;
            IconMaskBrush       = iconMaskBrush;
            AccentColor         = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = $"{PathToIcons}/kusd.svg";
            DisabledIconPath    = $"{PathToIcons}/kusd-disabled.svg";
            FeeName             = Resources.SvMiningFee;
        }
    }
}

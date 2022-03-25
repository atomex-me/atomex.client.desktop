using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Avalonia.Visuals.Media.Imaging;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class EthereumCurrencyViewModel : CurrencyViewModel
    {
        public EthereumCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            var iconBrush = new ImageBrush(GetBitmap(PathToImage("ethereum_90x90.png")));
            var iconMaskBrush = new ImageBrush(GetBitmap(PathToImage("ethereum_mask.png")));
            iconBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            iconMaskBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            
            Header              = Currency.Description;
            IconBrush           = iconBrush;
            IconMaskBrush       = iconMaskBrush;
            AccentColor         = Color.FromRgb(r: 73, g: 114, b: 143);
            AmountColor         = Color.FromRgb(r: 183, g: 208, b: 225);
            UnselectedIconBrush = Brushes.White;
            IconPath            = $"{PathToIcons}/ethereum.svg";
            DisabledIconPath    = $"{PathToIcons}/ethereum-disabled.svg";
            FeeName             = Resources.SvGasLimit;
        }
    }
}
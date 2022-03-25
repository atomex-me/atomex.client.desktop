using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Avalonia.Visuals.Media.Imaging;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TezosCurrencyViewModel : CurrencyViewModel
    {
        public TezosCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush           = DefaultIconBrush;
            IconMaskBrush       = DefaultIconMaskBrush;
            AccentColor         = Color.FromRgb(r: 44, g: 125, b: 247);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = $"{PathToIcons}/tezos.svg";
            DisabledIconPath    = $"{PathToIcons}/tezos-disabled.svg";
            FeeName             = Resources.SvMiningFee;
        }
        private static ImageBrush _defaultIconBrush  = new ImageBrush(GetBitmap(PathToImage("tezos_90x90.png")));
        private static ImageBrush _defaultIconMaskBrush = new ImageBrush(GetBitmap(PathToImage("tezos_mask.png")));

        public static Brush DefaultIconBrush {
            get
            {
                _defaultIconBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
                return _defaultIconBrush;
            }
        }

        public static Brush DefaultIconMaskBrush
        {
            get
            {
                _defaultIconMaskBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
                return _defaultIconMaskBrush;
            }
        }
        
        public static IBrush DefaultUnselectedIconBrush = Brushes.White;
    }
}
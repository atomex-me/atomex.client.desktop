using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class KusdCurrencyViewModel : CurrencyViewModel
    {
        public KusdCurrencyViewModel(Currency currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush           = new ImageBrush(GetBitmap(PathToImage("kusd_90x90.png")));
            IconMaskBrush       = new ImageBrush(GetBitmap(PathToImage("kusd_mask.png")));
            AccentColor         = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = GetBitmap(PathToImage("kusd.png"));
            LargeIconPath       = GetBitmap(PathToImage("kusd_90x90.png"));
            FeeName             = Resources.SvMiningFee;
        }
    }
}
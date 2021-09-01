using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TzbtcCurrencyViewModel : CurrencyViewModel
    {
        public TzbtcCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush           = new ImageBrush(GetBitmap(PathToImage("tzbtc_90x90_dark.png")));
            IconMaskBrush       = new ImageBrush(GetBitmap(PathToImage("tzbtc_mask.png")));
            AccentColor         = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = GetBitmap(PathToImage("tzbtc_dark.png"));
            LargeIconPath       = GetBitmap(PathToImage("tzbtc_90x90_dark.png"));
            FeeName             = Resources.SvMiningFee;
        }
    }
}
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class LitecoinCurrencyViewModel : CurrencyViewModel
    {
        public LitecoinCurrencyViewModel(Currency currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush = new ImageBrush(
                GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/litecoin_90x90.png"));
            IconMaskBrush =
                new ImageBrush(GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/litecoin_mask.png"));
            AccentColor         = Color.FromRgb(r: 191, g: 191, b: 191);
            AmountColor         = Color.FromRgb(r: 231, g: 231, b: 231);
            UnselectedIconBrush = Brushes.White;
            IconPath            = PathToImage("litecoin.png");
            LargeIconPath       = PathToImage("litecoin_90x90.png");
            FeeName             = Resources.SvMiningFee;
        }
    }
}
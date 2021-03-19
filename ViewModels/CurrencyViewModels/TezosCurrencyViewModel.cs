using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TezosCurrencyViewModel : CurrencyViewModel
    {
        public TezosCurrencyViewModel(Currency currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush = new ImageBrush(
                GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/tezos_90x90.png"));
            IconMaskBrush =
                new ImageBrush(GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/tezos_mask.png"));
            AccentColor         = Color.FromRgb(r: 44, g: 125, b: 247);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = PathToImage("tezos.png");
            LargeIconPath       = PathToImage("tezos_90x90.png");
            FeeName             = Resources.SvMiningFee;
        }
    }
}
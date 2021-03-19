using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class NYXCurrencyViewModel : CurrencyViewModel
    {
        public NYXCurrencyViewModel(Currency currency)
            : base(currency)
        {
            Header = Currency.Description;
            IconBrush = new ImageBrush(
                GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/tezos.png"));
            IconMaskBrush =
                new ImageBrush(GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/tezos_mask.png"));
            AccentColor = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath = PathToImage("tezos.png");
            LargeIconPath = PathToImage("tezos_90x90.png");
            FeeName = Resources.SvMiningFee;
        }
    }
}
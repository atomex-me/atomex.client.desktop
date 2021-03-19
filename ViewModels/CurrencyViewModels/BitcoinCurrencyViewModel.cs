using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class BitcoinCurrencyViewModel : CurrencyViewModel
    {
        public BitcoinCurrencyViewModel(Currency currency)
            : base(currency)
        {
            Header = Currency.Description;
            IconBrush = new ImageBrush(GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/bitcoin_90x90.png"));
            IconMaskBrush =
                new ImageBrush(GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/bitcoin_mask.png"));
            AccentColor = Color.FromRgb(r: 255, g: 148, b: 0);
            AmountColor = Color.FromRgb(r: 255, g: 148, b: 0);
            UnselectedIconBrush = Brushes.White;
            IconPath = PathToImage("bitcoin.png");
            LargeIconPath = PathToImage("bitcoin_90x90.png");
            FeeName = Resources.SvMiningFee;
        }
    }
}
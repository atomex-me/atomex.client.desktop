using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class BitcoinCurrencyViewModel : CurrencyViewModel
    {
        public BitcoinCurrencyViewModel(Currency currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush           = new ImageBrush(GetBitmap(PathToImage("bitcoin_90x90.png")));
            IconMaskBrush       = new ImageBrush(GetBitmap(PathToImage("bitcoin_mask.png")));
            AccentColor         = Color.FromRgb(r: 255, g: 148, b: 0);
            AmountColor         = Color.FromRgb(r: 255, g: 148, b: 0);
            UnselectedIconBrush = Brushes.White;
            IconPath            = GetBitmap(PathToImage("bitcoin.png"));
            LargeIconPath       = GetBitmap(PathToImage("bitcoin_90x90.png"));
            FeeName             = Resources.SvMiningFee;
        }
    }
}
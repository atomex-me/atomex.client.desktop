using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TbtcCurrencyViewModel : CurrencyViewModel
    {
        public decimal AvailableAmountInChainCurrency { get; set; }

        public TbtcCurrencyViewModel(Currency currency)
            : base(currency)
        {
            ChainCurrency = new Ethereum();
            Header = Currency.Description;
            IconBrush = new ImageBrush(
                GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/tbtc_90x90_dark.png"));
            IconMaskBrush =
                new ImageBrush(GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/tbtc_mask.png"));
            AccentColor = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath = GetBitmap(PathToImage("tbtc_dark.png"));
            LargeIconPath = GetBitmap(PathToImage("tbtc_90x90_dark.png"));
            FeeName = Resources.SvGasLimit;
        }
    }
}
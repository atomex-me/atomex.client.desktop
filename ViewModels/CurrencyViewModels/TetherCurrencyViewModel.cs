using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TetherCurrencyViewModel : CurrencyViewModel
    {
        public decimal AvailableAmountInChainCurrency { get; set; }

        public TetherCurrencyViewModel(Currency currency)
            : base(currency)
        {
            ChainCurrency       = new Ethereum();
            Header              = Currency.Description;
            IconBrush = new ImageBrush(
                GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/tether_90x90.png"));
            IconMaskBrush =
                new ImageBrush(GetBitmap("avares://Atomex.Client.Desktop/Resources/Images/tether_mask.png"));
            AccentColor         = Color.FromRgb(r: 0, g: 162, b: 122);
            AmountColor         = Color.FromRgb(r: 183, g: 208, b: 225);
            UnselectedIconBrush = Brushes.White;
            IconPath            = PathToImage("tether.png");
            LargeIconPath       = PathToImage("tether_90x90.png");
            FeeName             = Resources.SvGasLimit;
        }
    }
}
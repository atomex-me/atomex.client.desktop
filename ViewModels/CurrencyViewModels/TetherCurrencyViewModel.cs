using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;


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
            IconBrush           = new ImageBrush(GetBitmap(PathToImage("tether_90x90.png")));
            IconMaskBrush       = new ImageBrush(GetBitmap(PathToImage("tether_mask.png")));
            AccentColor         = Color.FromRgb(r: 0, g: 162, b: 122);
            AmountColor         = Color.FromRgb(r: 183, g: 208, b: 225);
            UnselectedIconBrush = Brushes.White;
            IconPath            = GetBitmap(PathToImage("tether.png"));
            LargeIconPath       = GetBitmap(PathToImage("tether_90x90.png"));
            FeeName             = Resources.SvGasLimit;
        }
    }
}
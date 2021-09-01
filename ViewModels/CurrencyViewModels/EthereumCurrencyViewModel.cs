using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;

namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class EthereumCurrencyViewModel : CurrencyViewModel
    {
        public EthereumCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush           = new ImageBrush(GetBitmap(PathToImage("ethereum_90x90.png")));
            IconMaskBrush       = new ImageBrush(GetBitmap(PathToImage("ethereum_mask.png")));
            AccentColor         = Color.FromRgb(r: 73, g: 114, b: 143);
            AmountColor         = Color.FromRgb(r: 183, g: 208, b: 225);
            UnselectedIconBrush = Brushes.White;
            IconPath            = GetBitmap(PathToImage("ethereum.png"));
            LargeIconPath       = GetBitmap(PathToImage("ethereum_90x90.png"));
            FeeName             = Resources.SvGasLimit;
        }
    }
}
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class TezosCurrencyViewModel : CurrencyViewModel
    {
        public TezosCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush           = new ImageBrush(GetBitmap(PathToImage("tezos_90x90.png")));
            IconMaskBrush       = new ImageBrush(GetBitmap(PathToImage("tezos_mask.png")));
            AccentColor         = Color.FromRgb(r: 44, g: 125, b: 247);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = GetBitmap(PathToImage("tezos.png"));
            LargeIconPath       = GetBitmap(PathToImage("tezos_90x90.png"));
            FeeName             = Resources.SvMiningFee;
        }
    }
}
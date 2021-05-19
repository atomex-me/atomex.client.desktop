using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class FA2CurrencyViewModel : CurrencyViewModel
    {
        public FA2CurrencyViewModel(Currency currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush           = new ImageBrush(GetBitmap(PathToImage("tezos_90x90.png")));
            IconMaskBrush       = new ImageBrush(GetBitmap(PathToImage("tezos_mask.png")));
            AccentColor         = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = GetBitmap(PathToImage("tezos.png"));
            LargeIconPath       = GetBitmap(PathToImage("tezos_90x90.png"));
            FeeName             = Resources.SvMiningFee;
        }
    }
}
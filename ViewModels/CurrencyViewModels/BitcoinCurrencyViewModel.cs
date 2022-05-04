using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Wallet.BitcoinBased;
using Avalonia.Visuals.Media.Imaging;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class BitcoinCurrencyViewModel : CurrencyViewModel
    {
        public BitcoinCurrencyViewModel(CurrencyConfig_OLD currency)
            : base(currency)
        {
            var iconBrush                         = new ImageBrush(GetBitmap(PathToImage("bitcoin_90x90.png")));
            var iconMaskBrush                     = new ImageBrush(GetBitmap(PathToImage("bitcoin_mask.png")));
            iconBrush.BitmapInterpolationMode     = BitmapInterpolationMode.HighQuality;
            iconMaskBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            
            Header              = Currency.Description;
            IconBrush           = iconBrush;
            IconMaskBrush       = iconMaskBrush;
            AccentColor         = Color.FromRgb(r: 255, g: 148, b: 0);
            AmountColor         = Color.FromRgb(r: 255, g: 148, b: 0);
            UnselectedIconBrush = Brushes.White;
            IconPath            = GetBitmap(PathToImage("bitcoin.png"));
            LargeIconPath       = GetBitmap(PathToImage("bitcoin_90x90.png"));
            FeeName             = Resources.SvMiningFee;
        }
    }
}
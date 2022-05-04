﻿using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Avalonia.Visuals.Media.Imaging;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class KusdCurrencyViewModel : CurrencyViewModel
    {
        public KusdCurrencyViewModel(CurrencyConfig_OLD currency)
            : base(currency)
        {
            var iconBrush = new ImageBrush(GetBitmap(PathToImage("kusd_90x90.png")));
            var iconMaskBrush = new ImageBrush(GetBitmap(PathToImage("kusd_mask.png")));
            iconBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            iconMaskBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            
            Header              = Currency.Description;
            IconBrush           = iconBrush;
            IconMaskBrush       = iconMaskBrush;
            AccentColor         = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = GetBitmap(PathToImage("kusd.png"));
            LargeIconPath       = GetBitmap(PathToImage("kusd_90x90.png"));
            FeeName             = Resources.SvMiningFee;
        }
    }
}

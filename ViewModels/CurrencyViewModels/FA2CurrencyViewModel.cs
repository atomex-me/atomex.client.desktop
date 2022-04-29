﻿using Atomex.Client.Desktop.Properties;
using Atomex.Core;
using Avalonia.Media;
using Avalonia.Visuals.Media.Imaging;


namespace Atomex.Client.Desktop.ViewModels.CurrencyViewModels
{
    public class FA2CurrencyViewModel : CurrencyViewModel
    {
        public FA2CurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            var iconBrush = new ImageBrush(GetBitmap(PathToImage("tezos_90x90.png")));
            var iconMaskBrush = new ImageBrush(GetBitmap(PathToImage("tezos_mask.png")));
            iconBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            iconMaskBrush.BitmapInterpolationMode = BitmapInterpolationMode.HighQuality;
            
            Header              = Currency.Description;
            IconBrush           = iconBrush;
            IconMaskBrush       = iconMaskBrush;
            AccentColor         = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = $"{PathToIcons}/tezos.svg";
            DisabledIconPath    = $"{PathToIcons}/tezos-disabled.svg";
            FeeName             = Resources.SvMiningFee;
        }
    }
}
﻿using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia.Data.Converters;
using Atomex.Client.Desktop.Common;
using Avalonia;

namespace Atomex.Client.Desktop.Converters
{
    public class DialogOverlayerMarginConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return  RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? new Thickness(0)
                    : new Thickness(0, 30, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
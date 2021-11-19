using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class WalletsViewMarginConverter : IValueConverter
    {
        #region IValueConverter Members

        private const double TITLEBAR_HEIGHT = 30;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If Is Linux == true
            return value is true ? new Thickness(0, TITLEBAR_HEIGHT, 0, 0) : new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
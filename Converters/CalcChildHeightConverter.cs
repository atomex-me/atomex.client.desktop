using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class CalcChildHeightConverter : IValueConverter
    {
        #region IValueConverter Members

        private const double TITLEBAR_HEIGHT = 30;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return val - TITLEBAR_HEIGHT;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
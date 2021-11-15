using System;
using System.Globalization;
using System.Runtime.InteropServices;

using Avalonia.Data.Converters;
using Atomex.Client.Desktop.Common;
using Avalonia;

namespace Atomex.Client.Desktop.Converters
{
    public class DialogOpenedToBorderThicknessConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool opened)
            {
                return opened ? new Thickness(0) : new Thickness(0, 0, 1, 3);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
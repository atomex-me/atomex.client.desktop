using System;
using System.Globalization;
using Avalonia.Data.Converters;


namespace Atomex.Client.Desktop.Converters
{
    public class AddressShouldExpandConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val)
                return val >= AddressShouldShrinkConverter.AddressViewWidthToChange;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
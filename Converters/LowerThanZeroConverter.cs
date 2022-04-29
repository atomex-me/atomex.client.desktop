using System;
using System.Globalization;
using Avalonia.Data.Converters;


namespace Atomex.Client.Desktop.Converters
{
    public class LowerThanZeroConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal val)
                return val < 0;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
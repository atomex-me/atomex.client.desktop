using System;
using System.Globalization;
using Avalonia.Data.Converters;


namespace Atomex.Client.Desktop.Converters
{
    public class BoolToRowSpanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasDescription)
                return hasDescription ? 1 : 2;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}

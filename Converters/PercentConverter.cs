using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class PercentConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue && targetType == typeof(string))
            {
                decimal percentValue = decimalValue * 100;
                return $"{percentValue:F}%";
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
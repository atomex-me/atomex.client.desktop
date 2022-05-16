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
            if (value is not decimal decimalValue || targetType != typeof(string)) return value;
            
            var percentValue = decimalValue * 100;
            return percentValue != 0 ? $"{percentValue:F}%" : "0%";

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
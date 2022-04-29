using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class DateTimeToTxTimeConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime time && targetType == typeof(string))
                return time.ToString("MMM dd yyyy, hh:mm", CultureInfo.CurrentCulture) +
                       $" {time.ToString("tt").ToLower()}";

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        #endregion
    }
}
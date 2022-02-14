using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class EnumToStringEqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class StringToLongConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not long longValue)
                return string.Empty;

            return longValue.ToString(CultureInfo.CurrentCulture);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string stringValue || string.IsNullOrEmpty(stringValue))
                return 0;

            return long.Parse(stringValue, CultureInfo.CurrentCulture);
        }
    }
}
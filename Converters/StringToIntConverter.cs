using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class StringToIntConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not int intValue)
                return string.Empty;

            return intValue.ToString(CultureInfo.CurrentCulture);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string stringValue || string.IsNullOrEmpty(stringValue))
                return 0;

            return int.Parse(stringValue, CultureInfo.CurrentCulture);
        }
    }
}
using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class StringToDecimalConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not decimal decimalValue)
                return string.Empty;

            return decimalValue.ToString(CultureInfo.CurrentCulture);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string stringValue || string.IsNullOrEmpty(stringValue))
                return 0m;

            return decimal.Parse(stringValue, CultureInfo.CurrentCulture);
        }
    }
}
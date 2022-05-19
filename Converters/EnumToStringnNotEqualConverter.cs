using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class EnumToStringNotEqualConverter : IValueConverter
    {
        public static readonly EnumToStringNotEqualConverter Instance = new();

        private EnumToStringNotEqualConverter()
        {
        }

        object IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString() != parameter?.ToString();
        }

        object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;


namespace Atomex.Client.Desktop.Converters
{
    public class StringsConcatConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (values[i] is not string str || str == AvaloniaProperty.UnsetValue) return string.Empty;
            }

            return values.Aggregate(string.Empty, (s, o) => $"{s}{o}");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;


namespace Atomex.Client.Desktop.Converters
{
    public class StringsEqualConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is not { Count: 2 })
                throw new InvalidOperationException("Invalid values");

            if (values[0] != AvaloniaProperty.UnsetValue && values[1] != AvaloniaProperty.UnsetValue)
                return string.Equals(values[0]?.ToString(), values[1]?.ToString());

            return false;
        }
    }
}
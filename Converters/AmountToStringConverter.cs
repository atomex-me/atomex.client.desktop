using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;


namespace Atomex.Client.Desktop.Converters
{
    public class AmountToStringConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is { Count: 2 })
            {
                if (values[0] == AvaloniaProperty.UnsetValue || values[1] == AvaloniaProperty.UnsetValue)
                    return "-";
            }

            if (values is { Count: 3 })
            {
                if (values[0] == AvaloniaProperty.UnsetValue ||
                    values[1] == AvaloniaProperty.UnsetValue ||
                    values[2] == AvaloniaProperty.UnsetValue)
                    return "-";
            }

            var amount = (decimal)values[0];
            var format = (string)values[1];

            if (values is { Count: 3 } && (bool)values[2] && amount > 0)
                return $"+{amount.ToString(format, culture)}";
            
            return amount.ToString(format, culture);
        }
    }
}
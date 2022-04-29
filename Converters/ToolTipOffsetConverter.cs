using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;


namespace Atomex.Client.Desktop.Converters
{
    public class ToolTipOffsetConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not { Count: 2 }) return 0;

            if (values[0] == AvaloniaProperty.UnsetValue || values[1] == AvaloniaProperty.UnsetValue)
                return 0;

            if (values[0] is not double controlHeight || values[1] is not double tipHeight || controlHeight == 0 ||
                tipHeight == 0) return 0;

            return -((controlHeight + tipHeight) / 2) - 16;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class WidthToHorizontalCenterOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var doubleValue = (double)value;

            if (parameter != null && double.TryParse(parameter.ToString(), out var offset))
                return -doubleValue / 2 + offset;

            return -doubleValue / 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
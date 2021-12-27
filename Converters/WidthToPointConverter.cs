using System;
using System.Globalization;

using Avalonia;
using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class WidthToPointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
                return new Point(width, 1);

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
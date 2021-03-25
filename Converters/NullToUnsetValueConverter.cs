using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;


namespace Atomex.Client.Desktop.Converters
{
    public class NullToUnsetValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value ?? AvaloniaProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }
    }
}
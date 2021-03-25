using System;
using System.Globalization;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;


namespace Atomex.Client.Desktop.Converters
{
    public class CanScrollConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ScrollBarVisibility scrollBarVisibility)
            {
                Console.WriteLine($"Converting scrollbar visibility {scrollBarVisibility}");
                return scrollBarVisibility != ScrollBarVisibility.Disabled;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ScrollBarVisibility scrollBarVisibility)
            {
                return scrollBarVisibility != ScrollBarVisibility.Disabled;
            }

            return value;
        }
    }
}
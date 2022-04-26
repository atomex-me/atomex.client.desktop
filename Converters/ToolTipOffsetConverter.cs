using System;
using System.Globalization;

using Avalonia;
using Avalonia.Data.Converters;
using Serilog;

namespace Atomex.Client.Desktop.Converters
{
    public class ToolTipOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double controlHeight) return value;
            return 0 - (controlHeight + 12);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
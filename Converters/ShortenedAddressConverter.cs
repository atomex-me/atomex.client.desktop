using System;
using System.Globalization;
using Atomex.ViewModels;
using Avalonia;
using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class ShortenedAddressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string address)
            {
                return address.TruncateAddress(15, 12);
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
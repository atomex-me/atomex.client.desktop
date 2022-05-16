using System;
using System.Globalization;
using Atomex.Client.Desktop.ViewModels;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Atomex.ViewModels;
using Avalonia;
using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class ShortenedAddressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string address) return value;

            if (parameter is not AddressTruncateType truncateType)
                return address.TruncateAddress(15, 12);

            return truncateType switch
            {
                AddressTruncateType.Short => address.TruncateAddress(6, 4),
                _ => address.TruncateAddress(15, 12)
            };

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
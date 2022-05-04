using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class BitcoinOutputValueToCoin : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is not { Count: 2 } || values[0] is not long satoshi ||
                values[1] is not BitcoinBasedConfig_OLD bitcoinBasedConfig)
                throw new InvalidOperationException("Invalid values");

            return bitcoinBasedConfig.SatoshiToCoin(satoshi);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Array.Empty<object>();
        }
    }
}
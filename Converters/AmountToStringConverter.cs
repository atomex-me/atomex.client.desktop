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
            if (values == null || values.Count != 2)
                throw new InvalidOperationException("Invalid values");

            if (values[0] == AvaloniaProperty.UnsetValue || values[1] == AvaloniaProperty.UnsetValue)
                return "-";

            var amount = (decimal) values[0];
            var format = (string) values[1];

            return amount.ToString(format, culture);
        }

        //public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        //{
        //    if (value is not string s)
        //        throw new InvalidOperationException("Invalid value");

        //    return new object[]
        //    {
        //        string.IsNullOrEmpty(s) ? 0.0m : decimal.Parse(s, culture),
        //        null
        //    };
        //}
    }
}
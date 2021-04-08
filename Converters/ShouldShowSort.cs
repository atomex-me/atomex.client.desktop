using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using NBitcoin;


namespace Atomex.Client.Desktop.Converters
{
    public class ShouldShowSort : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Count != 2)
                throw new InvalidOperationException("Invalid values");

            if (values[0] == AvaloniaProperty.UnsetValue || values[1] == AvaloniaProperty.UnsetValue ||
                values[0] == null || values[1] == null)
                return false;

            return String.Equals(values[0].ToString()!, values[1].ToString()!,
                StringComparison.CurrentCultureIgnoreCase);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (!(value is string s))
                throw new InvalidOperationException("Invalid value");

            return new object[]
            {
                string.IsNullOrEmpty(s) ? 0.0m : decimal.Parse(s, culture),
                null
            };
        }
    }
}
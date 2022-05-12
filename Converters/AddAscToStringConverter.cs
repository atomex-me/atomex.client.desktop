using System;
using System.Globalization;
using Atomex.Client.Desktop.ViewModels.Abstract;
using Avalonia.Data.Converters;
using Atomex.Client.Desktop.ViewModels.WalletViewModels;

namespace Atomex.Client.Desktop.Converters
{
    public class AddAscToStringConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string val)
            {
                return $"{val}/{SortDirection.Asc}";
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
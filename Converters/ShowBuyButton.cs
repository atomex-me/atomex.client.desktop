using System;
using System.Globalization;
using System.Linq;
using Atomex.Client.Desktop.ViewModels;
using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class ShowBuyButton : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string currencyCode)
                return WertViewModel.CurrenciesToBuy.Contains(currencyCode);

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
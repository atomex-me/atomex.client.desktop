using System;
using System.Globalization;
using Atomex.Client.Desktop.ViewModels;
using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public class PortfolioToTotalConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal totalValue && targetType == typeof(string))
                return
                    $"${totalValue.ToString(PortfolioViewModel.GetAmountFormat(totalValue), CultureInfo.CurrentCulture)}";

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
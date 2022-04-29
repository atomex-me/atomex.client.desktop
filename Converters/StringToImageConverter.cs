using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Atomex.Client.Desktop.ViewModels.WalletViewModels;

namespace Atomex.Client.Desktop.Converters
{
    public class StringToImageConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringUrl)
            {
                return App.ImageService.GetImage(stringUrl);
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
using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Atomex.Client.Desktop.Converters
{
    public class URIToImageConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && targetType == typeof(IImage))

            {
                Console.WriteLine($"Converting IMAGE {value}");
                IAssetLoader assets;
                assets = AvaloniaLocator.Current.GetService<IAssetLoader>();

                var bitmap = new Bitmap(assets.Open(new Uri((string) value)));
                return bitmap;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;


namespace Atomex.Client.Desktop.Converters
{
    public static class ImageConverters
    {
        public static readonly IValueConverter ToImage =
            new FuncValueConverter<string, IBitmap>(imageSource => App.ImageService.GetImage(imageSource));
    }
}
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Atomex.Client.Desktop.Converters
{
    public static class StringConverters
    {
        public static readonly IValueConverter ToImage =
            new FuncValueConverter<string, IBitmap>(stringUrl => App.ImageService.GetImage(stringUrl!));
    }
}
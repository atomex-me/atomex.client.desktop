using Avalonia.Data.Converters;

using Atomex.Common;

namespace Atomex.Client.Desktop.Converters
{
    public static class StringConverters
    {
        public static readonly IValueConverter ToShortenedAddress = new FuncValueConverter<string, string>(
            address => address.TruncateAddress());
        
        public static readonly IValueConverter ToLongShortenedAddress = new FuncValueConverter<string, string>(
            address => address.TruncateAddress(15, 12));
    }
}
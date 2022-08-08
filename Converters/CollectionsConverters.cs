using System.Collections;
using Avalonia.Data.Converters;

namespace Atomex.Client.Desktop.Converters
{
    public static class CollectionsConverters
    {
        public static readonly IValueConverter MoreZero =
            new FuncValueConverter<ICollection?, bool>(collection => collection is { Count: > 0 });
    }
}
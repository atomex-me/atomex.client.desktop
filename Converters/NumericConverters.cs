using Avalonia.Data.Converters;


namespace Atomex.Client.Desktop.Converters
{
    public static class NumericConverters
    {
        public static readonly IValueConverter GreaterThanZero =
            new FuncValueConverter<object, bool>(val =>
            {
                return val switch
                {
                    decimal decimalValue => decimalValue > 0,
                    int intValue => intValue > 0,
                    _ => false
                };
            });

        public static readonly IValueConverter LowerThanZero =
            new FuncValueConverter<object, bool>(val =>
            {
                return val switch
                {
                    decimal decimalValue => decimalValue < 0,
                    int intValue => intValue < 0,
                    _ => false
                };
            });
        
        public static readonly IValueConverter NotZero =
            new FuncValueConverter<object, bool>(val =>
            {
                return val switch
                {
                    decimal decimalValue => decimalValue != 0,
                    int intValue => intValue != 0,
                    _ => false
                };
            });
    }
}
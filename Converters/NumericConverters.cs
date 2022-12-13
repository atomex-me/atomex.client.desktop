using Avalonia.Data.Converters;


namespace Atomex.Client.Desktop.Converters
{
    public static class NumericConverters
    {
        public static readonly IValueConverter GreaterThanZero = new FuncValueConverter<object, bool>(val =>
        {
            return val switch
            {
                decimal decimalValue => decimalValue > 0,
                int intValue => intValue > 0,
                _ => false
            };
        });

        public static readonly IValueConverter LowerThanZero = new FuncValueConverter<object, bool>(val =>
        {
            return val switch
            {
                decimal decimalValue => decimalValue < 0,
                int intValue => intValue < 0,
                _ => false
            };
        });

        public static readonly IValueConverter NotZero = new FuncValueConverter<object, bool>(val =>
        {
            return val switch
            {
                decimal decimalValue => decimalValue != 0,
                int intValue => intValue != 0,
                long longValue => longValue != 0,
                _ => false
            };
        });

        public static readonly IValueConverter IsZero = new FuncValueConverter<object, bool>(val =>
        {
            return val switch
            {
                decimal decimalValue => decimalValue == 0,
                int intValue => intValue == 0,
                long longValue => longValue == 0,
                _ => false
            };
        });

        public static readonly IValueConverter GreaterThanOne = new FuncValueConverter<object, bool>(val =>
        {
            return val switch
            {
                decimal decimalValue => decimalValue > 1,
                int intValue => intValue > 1,
                _ => false
            };
        });

        public static readonly IValueConverter IsOne = new FuncValueConverter<object, bool>(val =>
        {
            return val switch
            {
                decimal decimalValue => decimalValue == 1,
                int intValue => intValue == 1,
                _ => false
            };
        });
        
        public static readonly IValueConverter IsNotOne = new FuncValueConverter<object, bool>(val =>
        {
            return val switch
            {
                decimal decimalValue => decimalValue != 1,
                int intValue => intValue != 1,
                _ => false
            };
        });
    }
}
using System;
using System.Globalization;

namespace Atomex.Client.Desktop.Common
{
    public static class DecimalExtensions
    {
        public static decimal TruncateByFormat(this decimal d, string format)
        {
            var s = d.ToString(format, CultureInfo.InvariantCulture);

            return decimal.Parse(s, CultureInfo.InvariantCulture);
        }

        public static decimal TruncateDecimal(this decimal value, int precision)
        {
            var step = (decimal)Math.Pow(10, precision);

            var integralPart = decimal.Truncate(value);
            var decimalPart = value - integralPart;

            return integralPart + Math.Truncate(step * decimalPart) / step;

            //var tmp = Math.Truncate(step * value);
            //return tmp / step;
        }
    }
}
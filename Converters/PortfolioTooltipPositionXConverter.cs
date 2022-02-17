using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;


namespace Atomex.Client.Desktop.Converters
{
    public class PortfolioTooltipPositionXConverter : IMultiValueConverter
    {
        private const int PortfolioValueTextMargin = 40;

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not { Count: 4 })
                throw new InvalidOperationException("Invalid values");

            if (values[0] == AvaloniaProperty.UnsetValue || values[1] == AvaloniaProperty.UnsetValue)
                return values[0];

            var mouseX = (double)values[0];
            var borderWidth = (double)values[1];
            var plotWith = (double)values[2];
            var plotHeight = (double)values[3];

            var leftRightPos = mouseX - borderWidth / 2;
            leftRightPos = Math.Max(0, leftRightPos);
            var rightOverflow = leftRightPos + borderWidth - plotWith / 2 - (plotHeight - 16) / 2 -
                                PortfolioValueTextMargin;

            if (rightOverflow > 0)
                leftRightPos -= rightOverflow;

            return leftRightPos;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
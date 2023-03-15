using System;
using System.Globalization;

using Avalonia.Data.Converters;

using Atomex.Client.Desktop.ViewModels;

namespace Atomex.Client.Desktop.Converters
{
    public class CompactStateToBoolConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SwapCompactState compactState)
            {
                var targetState = Enum.Parse<SwapCompactState>((string)parameter);

                return compactState == targetState;
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
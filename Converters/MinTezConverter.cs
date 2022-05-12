// using System;
// using System.Globalization;
// using Avalonia.Data.Converters;
//
// namespace Atomex.Client.Desktop.Converters
// {
//     public class MinTezConverter : IValueConverter
//     {
//         #region IValueConverter Members
//
//         public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
//         {
//             if (value is decimal str && targetType == typeof(string))
//                 return $"(min {str} tez)";
//
//             return value;
//         }
//
//         public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
//         {
//             return value;
//         }
//
//         #endregion
//     }
// }
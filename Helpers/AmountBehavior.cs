using System;
using System.Globalization;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

using Atomex.Common;

namespace Atomex.Client.Desktop.Helpers
{
    public class AmountBehavior : Behavior<TextBox>
    {
        public static readonly StyledProperty<string> FormatProperty =
            AvaloniaProperty.Register<AmountBehavior, string>(nameof(Format), "0.########");

        public static readonly StyledProperty<Type> NumericTypeProperty =
            AvaloniaProperty.Register<AmountBehavior, Type>(nameof(Type), typeof(decimal));

        public string Format
        {
            get => GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }

        public Type NumericType
        {
            get => GetValue(NumericTypeProperty);
            set => SetValue(NumericTypeProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject!.AddHandler(InputElement.TextInputEvent, TextInputEventHandler, RoutingStrategies.Tunnel);
            AssociatedObject!.AddHandler(InputElement.LostFocusEvent, LostFocusEventHandler);
            AssociatedObject!.AddHandler(TextBox.PastingFromClipboardEvent, PastingFromClipboardEventHandler);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject?.RemoveHandler(InputElement.TextInputEvent, TextInputEventHandler);
            AssociatedObject?.RemoveHandler(InputElement.LostFocusEvent, LostFocusEventHandler);
            AssociatedObject?.RemoveHandler(TextBox.PastingFromClipboardEvent, PastingFromClipboardEventHandler);
        }

        private void TextInputEventHandler(object? sender, TextInputEventArgs args)
        {
            if (AssociatedObject == null)
                return;

            args.Handled = AddText(AssociatedObject, args.Text);
        }

        private void LostFocusEventHandler(object? sender, RoutedEventArgs args)
        {
            if (string.IsNullOrEmpty(AssociatedObject!.Text))
            {
                AssociatedObject.Text = "0";
            }
        }

        private void PastingFromClipboardEventHandler(object? sender, RoutedEventArgs args)
        {
            var clipboardText = App.Clipboard
                .GetTextAsync()
                .WaitForResult();

            args.Handled = AddText(AssociatedObject!, clipboardText);
        }

        private bool AddText(TextBox textBox, string? text)
        {
            if (text == null)
                return true;

            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            var selectionStart = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
            var selectionEnd = Math.Max(textBox.SelectionStart, textBox.SelectionEnd);

            var prefix = textBox.Text[..selectionStart];
            var suffix = textBox.Text[selectionEnd..];
            var resultText = $"{prefix}{text}{suffix}";

            var allowDecimalSeparator = NumericType == typeof(decimal) ||
                NumericType == typeof(double) ||
                NumericType == typeof(float);

            // ignore text with invalid characters
            if (resultText.Any(c => !char.IsDigit(c) && (!allowDecimalSeparator || (allowDecimalSeparator && c.ToString() != decimalSeparator))))
                return true;

            if (NumericType == typeof(decimal))
            {
                // ignore text that cannot be parsed in decimal
                if (!string.IsNullOrEmpty(resultText) && !decimal.TryParse(resultText, out var _))
                    return true;
            }
            else if (NumericType == typeof(double))
            {
                // ignore text that cannot be parsed in double
                if (!string.IsNullOrEmpty(resultText) && !double.TryParse(resultText, out var _))
                    return true;
            }
            else if (NumericType == typeof(float))
            {
                // ignore text that cannot be parsed in float
                if (!string.IsNullOrEmpty(resultText) && !float.TryParse(resultText, out var _))
                    return true;
            }
            else if (NumericType == typeof(int))
            {
                // ignore text that cannot be parsed in int
                if (!string.IsNullOrEmpty(resultText) && !int.TryParse(resultText, out var _))
                    return true;
            }
            else if (NumericType == typeof(long))
            {
                // ignore text that cannot be parsed in long
                if (!string.IsNullOrEmpty(resultText) && !long.TryParse(resultText, out var _))
                    return true;
            }

            return false;
        }
    }
}
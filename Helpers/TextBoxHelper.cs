using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Atomex.Client.Desktop.Helpers
{
    public class TextBoxHelper : Visual
    {
        static TextBoxHelper()
        {
            AffectsRender<TextBoxHelper>(
                CornerRadiusProperty,
                IconProperty
            );
        }

        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            AvaloniaProperty.Register<TextBoxHelper, CornerRadius>(nameof(CornerRadius), new CornerRadius(20));

        public static readonly StyledProperty<object> IconProperty =
            AvaloniaProperty.Register<TextBoxHelper, object>("Icon");

        public static CornerRadius GetCornerRadius(TextBox comboBox) =>
            comboBox.GetValue(CornerRadiusProperty);

        public static void SetCornerRadius(TextBox comboBox, string value) =>
            comboBox.SetValue(CornerRadiusProperty, CornerRadius.Parse(value));

        public static object GetIcon(TextBox textBox) =>
            textBox.GetValue(IconProperty);

        public static void SetIcon(TextBox textBox, object value) =>
            textBox.SetValue(IconProperty, value);
    }
}
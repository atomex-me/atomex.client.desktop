using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Atomex.Client.Desktop.Helpers
{
    public class ComboBoxHelper : Visual
    {
        static ComboBoxHelper()
        {
            AffectsRender<ComboBoxHelper>(
                CornerRadiusProperty,
                PlaceHolderProperty
            );
        }
        
        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            AvaloniaProperty.Register<ComboBoxHelper, CornerRadius>(nameof(CornerRadius), new CornerRadius(0));

        public static readonly StyledProperty<string> PlaceHolderProperty =
            AvaloniaProperty.Register<ComboBoxHelper, string>("PlaceHolder", string.Empty);

        public static CornerRadius GetCornerRadius(ComboBox comboBox) =>
            (CornerRadius)comboBox.GetValue(CornerRadiusProperty);
        public static void SetCornerRadius(ComboBox comboBox, object value) =>
            comboBox.SetValue(CornerRadiusProperty, value);

        public static string GetPlaceHolder(ComboBox comboBox) =>
            (string)comboBox.GetValue(PlaceHolderProperty);
        public static void SetPlaceHolder(ComboBox comboBox, string value) =>
            comboBox.SetValue(PlaceHolderProperty, value);
    }
}
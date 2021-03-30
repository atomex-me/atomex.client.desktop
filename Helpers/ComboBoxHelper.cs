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
                PlaceHolderProperty,
                PopupPaddingProperty,
                IconColorProperty
            );
        }

        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            AvaloniaProperty.Register<ComboBoxHelper, CornerRadius>(nameof(CornerRadius), new CornerRadius(0));

        public static readonly StyledProperty<string> PlaceHolderProperty =
            AvaloniaProperty.Register<ComboBoxHelper, string>("PlaceHolder", string.Empty);

        public static readonly StyledProperty<Thickness> PopupPaddingProperty =
            AvaloniaProperty.Register<ComboBoxHelper, Thickness>("PopupPadding", new Thickness(20, 1, 20, 0));
        
        public static readonly StyledProperty<IBrush> IconColorProperty =
            AvaloniaProperty.Register<ComboBoxHelper, IBrush>("IconColor", Brushes.White);
        
        public static IBrush GetIconColor(ComboBox comboBox) =>
            comboBox.GetValue(IconColorProperty);
        public static void SetIconColor(ComboBox comboBox, IBrush value) =>
            comboBox.SetValue(IconColorProperty, value);
        
        public static Thickness GetPopupPadding(ComboBox comboBox) =>
            comboBox.GetValue(PopupPaddingProperty);
        public static void SetPopupPadding(ComboBox comboBox, string value) =>
            comboBox.SetValue(PopupPaddingProperty, Thickness.Parse(value));

        public static CornerRadius GetCornerRadius(ComboBox comboBox) =>
            comboBox.GetValue(CornerRadiusProperty);

        public static void SetCornerRadius(ComboBox comboBox, string value) =>
            comboBox.SetValue(CornerRadiusProperty, CornerRadius.Parse(value));

        public static string GetPlaceHolder(ComboBox comboBox) =>
            comboBox.GetValue(PlaceHolderProperty);

        public static void SetPlaceHolder(ComboBox comboBox, string value) =>
            comboBox.SetValue(PlaceHolderProperty, value);
    }
}
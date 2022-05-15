using Avalonia;
using Avalonia.Controls;

namespace Atomex.Client.Desktop.Controls
{
    public class RoundedButton : Button
    {
        static RoundedButton()
        {
            AffectsRender<RoundedButton>(
                CornerRadiusProperty
            );

            AffectsMeasure<RoundedButton>(
                CornerRadiusProperty
            );
        }

        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            AvaloniaProperty.Register<RoundedButton, CornerRadius>(nameof(CornerRadius), new CornerRadius(0));

        public CornerRadius CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
    }
}
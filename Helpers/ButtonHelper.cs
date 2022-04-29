using Avalonia;
using Avalonia.Controls;

namespace Atomex.Client.Desktop.Helpers
{
    public class ButtonHelper : Visual
    {
        static ButtonHelper()
        {
            AffectsRender<ButtonHelper>(
                IsLightProperty
            );
        }

        public static readonly StyledProperty<bool> IsLightProperty =
            AvaloniaProperty.Register<ButtonHelper, bool>("IsLight");
        
        public static bool GetIsLight(Button button) => button.GetValue(IsLightProperty);

        public static void SetIsLight(Button button, bool value) => button.SetValue(IsLightProperty, value);
    }
}
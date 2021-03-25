using Avalonia;
using Avalonia.Controls;

namespace Atomex.Client.Desktop.Controls
{
    public class CustomScrollViewer : ScrollViewer
    {
        static CustomScrollViewer()
        {
            AffectsRender<CustomScrollViewer>(
                HorizontalScrollVisibleProperty
            );
        }
        
        public static readonly StyledProperty<bool> HorizontalScrollVisibleProperty =
            AvaloniaProperty.Register<MenuButton, bool>("HorizontalScrollVisible", false);
        
        public bool HorizontalScrollVisible
        {
            get => GetValue(HorizontalScrollVisibleProperty);
            set => SetValue(HorizontalScrollVisibleProperty, value);
        }
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Atomex.Client.Desktop.Controls
{
    public class IconButton : Button
    {
        public static readonly StyledProperty<IBrush> MouseOverBrushProperty =
            AvaloniaProperty.Register<IconButton, IBrush>(nameof(MouseOverBrush), Brushes.White);

        public static readonly StyledProperty<IBrush> PressedBrushProperty =
            AvaloniaProperty.Register<IconButton, IBrush>(nameof(PressedBrush), Brushes.Gray);

        public static readonly StyledProperty<object> PathProperty =
            AvaloniaProperty.Register<IconButton, object>(nameof(Path));

        public IBrush MouseOverBrush
        {
            get => GetValue(MouseOverBrushProperty);
            set => SetValue(MouseOverBrushProperty, value);
        }

        public IBrush PressedBrush
        {
            get => GetValue(PressedBrushProperty);
            set => SetValue(PressedBrushProperty, value);
        }

        public object Path
        {
            get => GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }
    }
}
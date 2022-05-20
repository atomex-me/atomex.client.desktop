using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;


namespace Atomex.Client.Desktop.Controls
{
    public class IconButton : Button
    {
        static IconButton()
        {
            AffectsRender<IconButton>(
                ToolTextProperty,
                ContentProperty,
                IsActiveProperty,
                WithRedDotProperty
            );
        }
        
        public static readonly DirectProperty<IconButton, string> ToolTextProperty =
            AvaloniaProperty.RegisterDirect<IconButton, string>(
                nameof(ToolText),
                o => o.ToolText,
                (o, v) => o.ToolText = v,
                defaultBindingMode: BindingMode.TwoWay);

        private string _toolText;
        public string ToolText
        {
            get => _toolText;
            set => SetAndRaise(ToolTextProperty, ref _toolText, value);
        }
        
        
        public static readonly StyledProperty<bool> IsActiveProperty =
            AvaloniaProperty.Register<IconButton, bool>(nameof(IsActive));

        public bool IsActive
        {
            get => GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }
        
        public static readonly StyledProperty<bool> WithRedDotProperty =
            AvaloniaProperty.Register<IconButton, bool>(nameof(WithRedDot));

        public bool WithRedDot
        {
            get => GetValue(WithRedDotProperty);
            set => SetValue(WithRedDotProperty, value);
        }
    }
}
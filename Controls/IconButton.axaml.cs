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
                ContentProperty
            );
        }
        
        public static readonly DirectProperty<IconButton, string> ToolTextProperty =
            AvaloniaProperty.RegisterDirect<IconButton, string>(
                nameof(ToolText),
                o => o.ToolText,
                (o, v) => o.ToolText = v,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);

        private string _toolText;

        public string ToolText
        {
            get { return _toolText; }
            set { SetAndRaise(ToolTextProperty, ref _toolText, value); }
        }
    }
}
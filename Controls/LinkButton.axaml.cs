using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;


namespace Atomex.Client.Desktop.Controls
{
    public class LinkButton : Button
    {
        static LinkButton()
        {
            AffectsRender<LinkButton>(
                ToolTextProperty,
                UnderlinedProperty
            );
        }

        public static readonly DirectProperty<LinkButton, string> ToolTextProperty =
            AvaloniaProperty.RegisterDirect<LinkButton, string>(
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
        
        public static readonly DirectProperty<LinkButton, bool> UnderlinedProperty =
            AvaloniaProperty.RegisterDirect<LinkButton, bool>(
                nameof(Underlined),
                o => o.Underlined,
                (o, v) => o.Underlined = v);

        private bool _underlined;

        public bool Underlined
        {
            get { return _underlined; }
            set { SetAndRaise(UnderlinedProperty, ref _underlined, value); }
        }
    }
}
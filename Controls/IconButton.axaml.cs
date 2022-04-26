using Avalonia;
using Avalonia.Controls;


namespace Atomex.Client.Desktop.Controls
{
    public class IconButton : Button
    {
        static IconButton()
        {
            AffectsRender<IconButton>(
                ToolTextProperty
            );
        }


        public static readonly DirectProperty<IconButton, string> ToolTextProperty =
            AvaloniaProperty.RegisterDirect<IconButton, string>(
                nameof(ToolText),
                o => o.ToolText);

        private string _toolText;

        public string ToolText
        {
            get { return _toolText; }
            set { SetAndRaise(ToolTextProperty, ref _toolText, value); }
        }
    }
}
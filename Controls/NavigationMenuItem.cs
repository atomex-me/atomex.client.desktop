using System.Windows;
using Avalonia;

namespace Atomex.Client.Desktop.Controls
{
    
    // todo: remove
    public class NavigationMenuItem : AvaloniaObject
    {
        public string Header { get; set; }
        public object Icon { get; set; }
        public object Content { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
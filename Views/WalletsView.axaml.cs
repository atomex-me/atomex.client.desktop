using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views
{
    public class WalletsView : UserControl
    {
        public WalletsView()
        {
            InitializeComponent();
            
            // var scrollBar = this.FindControl<ScrollBar>("PART_HorizontalScrollBar123");

            var tabControl = this.FindControl<TabControl>("Wallets");
            var a = 5;

            // this.PropertyChanged += (s, e) =>
            // {
            //     if (e.Property == Control.DataContextProperty)
            //     {
            //     }
            // };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
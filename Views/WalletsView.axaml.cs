using System;
using Atomex.Client.Desktop.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Atomex.Client.Desktop.Views
{
    public class WalletsView : UserControl
    {
        public WalletsView()
        {
            InitializeComponent();
            
#if DEBUG
            if (!Design.IsDesignMode) return;

            var border = this.FindControl<Border>("MainControl");
            border.Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x21, 0x39));
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
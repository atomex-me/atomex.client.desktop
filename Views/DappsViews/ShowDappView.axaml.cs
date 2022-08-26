using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views.DappsViews
{
    public partial class ShowDappView : UserControl
    {
        public ShowDappView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
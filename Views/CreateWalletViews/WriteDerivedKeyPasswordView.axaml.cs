using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views.CreateWalletViews
{
    public class WriteDerivedKeyPasswordView : UserControl
    {
        public WriteDerivedKeyPasswordView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
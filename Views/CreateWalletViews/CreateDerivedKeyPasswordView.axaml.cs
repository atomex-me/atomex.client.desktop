using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views.CreateWalletViews
{
    public class CreateDerivedKeyPasswordView : UserControl
    {
        public CreateDerivedKeyPasswordView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
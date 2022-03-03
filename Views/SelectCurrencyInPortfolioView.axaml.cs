using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views
{
    public partial class SelectCurrencyInPortfolioView : UserControl
    {
        public SelectCurrencyInPortfolioView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
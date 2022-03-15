using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Atomex.Client.Desktop.ViewModels;

namespace Atomex.Client.Desktop.Views
{
    public partial class ConversionCurrencyView : UserControl
    {
        public ConversionCurrencyView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnGotFocus(object sender, GotFocusEventArgs args)
        {
            if (DataContext is ConversionCurrencyViewModel viewModel)
                viewModel.RaiseGotInputFocus();
        }
    }
}
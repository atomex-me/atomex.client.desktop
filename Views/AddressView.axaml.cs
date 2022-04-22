using Atomex.Client.Desktop.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views
{
    public partial class AddressView : UserControl
    {
        public AddressView()
        {
            InitializeComponent();

            var addressItemGrid = this.FindControl<Grid>("AddressItemGrid");

            PropertyChanged += (_, e) =>
            {
                if (e.Property == DataContextProperty && e.NewValue is AddressViewModel addressViewModel)
                {
                    addressItemGrid.ColumnDefinitions = addressViewModel.HasTokens
                        ? AddressesView.WithTokensColumns
                        : AddressesView.WithoutTokensColumns;
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
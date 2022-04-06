using Atomex.Client.Desktop.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Atomex.Client.Desktop.Views
{
    public class AddressesView : UserControl
    {
        public static ColumnDefinitions WithoutTokensColumns => new("13.2*,4*,2*,4*,0,1*,1.8*");
        public static ColumnDefinitions WithTokensColumns => new("13.2*,4*,2*,4*,4*,1*,1.8*");

        public AddressesView()
        {
            InitializeComponent();

            var headerGrid = this.FindControl<Grid>("HeaderGrid");

            PropertyChanged += (_, e) =>
            {
                if (e.Property == DataContextProperty && e.NewValue is AddressesViewModel addressesViewModel)
                {
                    headerGrid.ColumnDefinitions = addressesViewModel.HasTokens
                        ? WithTokensColumns
                        : WithoutTokensColumns;
                }
            };

#if DEBUG
            if (!Design.IsDesignMode) return;

            var designGrid = this.FindControl<Grid>("DesignGrid");
            designGrid.Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x21, 0x39));
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
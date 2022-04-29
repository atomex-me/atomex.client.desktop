using Atomex.Client.Desktop.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views
{
    public class WalletMainView : UserControl
    {
        public WalletMainView()
        {
            InitializeComponent();

            var walletContentGrid = this.FindControl<Grid>("WalletContentGrid");
            walletContentGrid.AddHandler(PointerPressedEvent, WalletContentGridClicked!, RoutingStrategies.Tunnel);

            void WalletContentGridClicked(object sender, PointerPressedEventArgs e)
            {
                if (DataContext is not WalletMainViewModel walletMainViewModel) return;
                if (!GetShouldClose(e.Source) || !walletMainViewModel.RightPopupOpened) return;
                
                walletMainViewModel.ShowRightPopupContent(null);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private bool GetShouldClose(IInteractive parent)
        {
            var control = parent as Control;
            if (control is WalletMainView) return true;
            return !control.Classes.Contains("NoCloseRightPopup") && GetShouldClose(parent.InteractiveParent);
        }
    }
}
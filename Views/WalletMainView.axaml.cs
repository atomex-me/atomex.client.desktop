using System;
using Atomex.Client.Desktop.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Atomex.Client.Desktop.Views
{
    public class WalletMainView : UserControl
    {
        public WalletMainView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void Popup_OnClosed(object? sender, EventArgs e)
        {
            if (DataContext is not WalletMainViewModel walletMainViewModel) return;
            walletMainViewModel.RightPopupContent = null;
        }
    }
}